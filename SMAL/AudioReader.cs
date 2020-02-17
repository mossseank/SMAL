/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using System.IO;

namespace SMAL
{
	/// <summary>
	/// Base type for classes that can load formatted audio data from a stream. This type manages the lifetime of the
	/// underlying stream.
	/// <para>
	/// This supports overflow buffering, for readers that must decode more samples than were requested. The overflow
	/// buffer will be read from until empty, before requesting more samples from the reader stream.
	/// </para>
	/// </summary>
	public abstract class AudioReader : ISampleSource, IDisposable
	{
		#region Fields
		/// <summary>
		/// The stream acting as a source of data for the reader.
		/// </summary>
		protected readonly Stream Stream;

		/// <summary>
		/// The number of frames available to read from the reader. A special value of <see cref="UInt32.MaxValue"/> is
		/// used for readers that support infinite reading.
		/// </summary>
		public abstract uint FrameCount { get; }

		/// <summary>
		/// The total number of frames that have been read.
		/// </summary>
		public uint Offset { get; private set; }

		/// <summary>
		/// The remaining number of frames available for reading. Returns <see cref="UInt32.MaxValue"/> for infinite
		/// readers.
		/// </summary>
		public uint Remaining => (FrameCount == UInt32.MaxValue) ? UInt32.MaxValue : FrameCount - Offset;

		/// <summary>
		/// The set of audio channels available in the data from this reader.
		/// </summary>
		public abstract AudioChannels Channels { get; }

		/// <summary>
		/// The number of channels in the audio data from this reader.
		/// </summary>
		public uint ChannelCount => (uint)Channels;

		/// <summary>
		/// The sample rate for the audio data from this reader.
		/// </summary>
		public abstract uint SampleRate { get; }

		#region Overflow Buffer
		/// <summary>
		/// The internal overflow buffer for samples.
		/// </summary>
		protected Span<byte> Overflow => _buffer;
		/// <summary>
		/// The overflow buffer, cast as <c>short</c>s.
		/// </summary>
		protected Span<short> OverflowShort => _buffer.AsSpan().UnsafeCast<short>();
		/// <summary>
		/// The overflow buffer, cast as <c>float</c>s.
		/// </summary>
		protected Span<float> OverflowFloat => _buffer.AsSpan().UnsafeCast<float>();

		private byte[] _buffer; // Overflow buffer
		private bool _bufferFloat; // If the samples in the overflow buffer are floats (false = shorts)
		private uint _bufferCount; // The total number of frames available in the buffer
		private uint _bufferOffset; // The read offset (in samples) into the overflow buffer
		#endregion // Overflow Buffer

		/// <summary>
		/// If the reader object has been disposed.
		/// </summary>
		protected bool IsDisposed { get; private set; } = false;
		#endregion // Fields
		
		/// <summary>
		/// Sets up the base objects of the audio reader.
		/// </summary>
		/// <param name="stream">The stream that this reader is sourcing data from.</param>
		/// <param name="overflowSize">The initial size of the overflow buffer, in bytes.</param>
		protected AudioReader(Stream stream, uint overflowSize)
		{
			Stream = stream ?? throw new ArgumentNullException(nameof(stream));
			if (!stream.CanRead)
				throw new ArgumentException($"AudioReader stream does not allow read operations", nameof(stream));

			_buffer = new byte[overflowSize];
			_bufferFloat = false;
			_bufferCount = 0;
			_bufferOffset = 0;
			Offset = 0;
		}
		~AudioReader()
		{
			if (!IsDisposed)
				OnDispose(false);
			IsDisposed = true;
		}

		#region Reading
		/// <summary>
		/// Reads samples from the stream and places them into the buffer as 16-bit signed interleaved LPCM.
		/// </summary>
		/// <param name="buffer">
		/// The buffer to place the samples info. Will be rounded down to the previous multiple of
		/// <see cref="ChannelCount"/>, if needed.
		/// </param>
		/// <returns>The total number of frames read from the stream.</returns>
		public uint GetSamples(Span<short> buffer)
		{
			// Validate and round
			buffer = buffer.Slice(0, buffer.Length - (int)(buffer.Length % ChannelCount));
			uint frames = (uint)buffer.Length / ChannelCount;
			if (frames == 0)
				return 0;
			uint total = 0;

			// Try overflow buffer
			if (_bufferCount > 0)
			{
				total = readFromOverflow(buffer.AsBytesUnsafe(), frames, false);
				frames -= total;
				buffer = buffer.Slice((int)(total * ChannelCount));
			}

			// Read from the stream
			if (frames > 0)
			{
				var readRes = ReadSamples(buffer.AsBytesUnsafe(), frames, false);
				total += readRes.Frames;
				updateOverflow(readRes.Overflow, readRes.OverflowFloat);
			}

			// Return
			Offset += total;
			return total;
		}

		/// <summary>
		/// Reads samples from the stream and places them into the buffer as 32-bit normalized interleaved LPCM.
		/// </summary>
		/// <param name="buffer">
		/// The buffer to place the samples info. Will be rounded down to the previous multiple of
		/// <see cref="ChannelCount"/>, if needed.
		/// </param>
		/// <returns>The total number of frames read from the stream.</returns>
		public uint GetSamples(Span<float> buffer)
		{
			// Validate and round
			buffer = buffer.Slice(0, buffer.Length - (int)(buffer.Length % ChannelCount));
			uint frames = (uint)buffer.Length / ChannelCount;
			if (frames == 0)
				return 0;
			uint total = 0;

			// Try overflow buffer
			if (_bufferCount > 0)
			{
				total = readFromOverflow(buffer.AsBytesUnsafe(), frames, true);
				frames -= total;
				buffer = buffer.Slice((int)(total * ChannelCount));
			}

			// Read from the stream
			if (frames > 0)
			{
				var readRes = ReadSamples(buffer.AsBytesUnsafe(), frames, true);
				total += readRes.Frames;
				updateOverflow(readRes.Overflow, readRes.OverflowFloat);
			}

			// Return
			Offset += total;
			return total;
		}

		/// <summary>
		/// Performs the reading logic from the stream, into the destination (and potentially overflow) buffer.
		/// </summary>
		/// <param name="dst">The buffer to write the samples into.</param>
		/// <param name="frames">The total number of frames to read.</param>
		/// <param name="dstFloat">
		/// If <paramref name="dst"/> should be filled with <c>float</c>s, <c>short</c>s otherwise.
		/// </param>
		/// <returns>
		/// Item 1: The number of frames written to <paramref name="dst"/>.
		/// Item 2: The number of frames written to the overflow buffer.
		/// Item 3: If the frames written to the overflow buffer are <c>float</c>s, <c>short</c>s otherwise.
		/// </returns>
		protected abstract (uint Frames, uint Overflow, bool OverflowFloat) ReadSamples
			(Span<byte> dst, uint frames, bool dstFloat);
		#endregion // Reading

		#region Overflow
		// Reads samples from the overflow buffer, returns the number of frames read
		private uint readFromOverflow(Span<byte> dst, uint frames, bool dstFloat)
		{
			frames = Math.Min(_bufferCount, frames);
			if (frames == 0)
				return 0;
			uint samps = frames * ChannelCount;

			if (_bufferFloat)
			{
				var src = OverflowFloat.Slice((int)_bufferOffset, (int)samps);
				if (dstFloat) // Direct Copy
					src.CopyTo(dst.UnsafeCast<float>());
				else // Float->Short convert
					SampleUtils.Convert(src, dst.UnsafeCast<short>());
			}
			else
			{
				var src = OverflowShort.Slice((int)_bufferOffset, (int)samps);
				if (dstFloat) // Short->Float convert
					SampleUtils.Convert(src, dst.UnsafeCast<float>());
				else
					src.CopyTo(dst.UnsafeCast<short>());
			}

			_bufferCount -= frames;
			_bufferOffset += samps;
			return frames;
		}

		// Updates the overflow buffer values
		private void updateOverflow(uint count, bool isFloat)
		{
			_bufferFloat = isFloat;
			_bufferCount = count;
			_bufferOffset = 0;
		}

		/// <summary>
		/// Ensures that the overflow buffer is at least <paramref name="size"/> bytes large. Note that this function
		/// does not save data in the existing overflow buffer.
		/// </summary>
		/// <param name="size">The minimum size of the buffer, in bytes.</param>
		protected void EnsureOverflowSize(uint size)
		{
			if (size > _buffer.LongLength)
			{
				_buffer = new byte[size];
				_bufferCount = 0;
				_bufferOffset = 0;
			}
		}
		#endregion // Overflow

		#region IDisposable
		public void Dispose()
		{
			if (!IsDisposed)
			{
				OnDispose(true);
				Stream.Dispose();
				GC.SuppressFinalize(this);
			}
			IsDisposed = true;
		}

		/// <summary>
		/// Called when the object is disposed to perform cleanup.
		/// </summary>
		/// <param name="disposing">If the call was through the <see cref="Dispose"/> function.</param>
		protected abstract void OnDispose(bool disposing);
		#endregion // IDisposable
	}
}
