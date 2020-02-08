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
	/// Base class for types that can decode audio from one of the <see cref="AudioEncoding"/> formats into interleaved
	/// linear pcm samples.
	/// </summary>
	public abstract class AudioDecoder : IDisposable
	{
		#region Fields
		/// <summary>
		/// The source audio encoding that this decoder processes.
		/// </summary>
		public abstract AudioEncoding Encoding { get; }
		/// <summary>
		/// Gets the audio channel layout for this decoder.
		/// </summary>
		public abstract AudioChannels Channels { get; }

		/// <summary>
		/// The number of channels (samples per frame) for this decoder.
		/// </summary>
		public uint ChannelCount => (uint)Channels;

		#region Buffer
		/// <summary>
		/// Untyped sample buffer for decoding - useful for decoders where samples must be decoded in chunks instead of
		/// one at a time.
		/// </summary>
		protected Span<byte> Buffer => _buffer.AsSpan();
		/// <summary>
		/// A view of <see cref="Buffer"/>, but typed to <see cref="float"/>.
		/// </summary>
		protected Span<float> FloatBuffer => _buffer.AsSpan().UnsafeCast<float>();
		/// <summary>
		/// A view of <see cref="Buffer"/>, but typed to <see cref="short"/>.
		/// </summary>
		protected Span<short> ShortBuffer => _buffer.AsSpan().UnsafeCast<short>();

		/// <summary>
		/// The total number of audio frames currently available in <see cref="Buffer"/>.
		/// </summary>
		protected uint BufferCount { get; private set; }
		/// <summary>
		/// The current read position within <see cref="Buffer"/>, in frames.
		/// </summary>
		protected uint BufferOffset { get; private set; }
		/// <summary>
		/// The number of frames remaining for reading within the internal buffer.
		/// </summary>
		public uint BufferRemaining => BufferCount - BufferOffset;
		/// <summary>
		/// If the samples in <see cref="Buffer"/> are floats, <c>false</c> implies they are shorts.
		/// </summary>
		protected bool BufferIsFloat { get; private set; }

		/// <summary>
		/// The size of a single audio frame in the buffer, a function of the channel count and buffer format.
		/// </summary>
		protected uint BufferFrameSize => ChannelCount * (BufferIsFloat ? 4u : 2u);

		private readonly byte[] _buffer; // Internal sample decode buffer
		#endregion // Buffer

		/// <summary>
		/// If this decoder has been disposed.
		/// </summary>
		protected bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Initializes the components of the base decoder type.
		/// </summary>
		/// <param name="bufferSize">
		/// The size (in bytes) of the internal frame buffer. This should be equal to the maximum number of bytes
		/// that the decoded format should ever have to buffer.
		/// </param>
		protected AudioDecoder(uint bufferSize)
		{
			_buffer = new byte[bufferSize];
			BufferCount = 0;
			BufferOffset = 0;
			BufferIsFloat = false;
		}
		~AudioDecoder()
		{
			if (!IsDisposed)
				OnDispose(false);
			IsDisposed = true;
		}

		/// <summary>
		/// Called to set the number and type of frames available in the internal buffer.
		/// </summary>
		/// <param name="frameCount">The number of frames to mark as available in the buffer.</param>
		/// <param name="isFloat">If the buffered samples are floats, otherwise they are shorts.</param>
		/// <exception cref="InsufficientMemoryException">The internal buffer is too small.</exception>
		protected void SetBufferCount(uint frameCount, bool isFloat)
		{
			ulong size = frameCount * (ulong)BufferFrameSize;
			if (size > (ulong)_buffer.LongLength)
			{
				throw new InsufficientMemoryException(
					$"Sample count ({Channels} {(isFloat?"float":"short")}[{frameCount}]) " +
					$"is too large for buffer ({_buffer.Length})");
			}

			BufferCount = frameCount;
			BufferOffset = 0;
			BufferIsFloat = isFloat;
		}

		#region Decode
		/// <summary>
		/// Decodes samples from the stream into the buffer. The stream should already be at the start of the audio
		/// data, any frames or containers from the stream format should already be parsed.
		/// </summary>
		/// <param name="stream">The stream to decode samples from.</param>
		/// <param name="buffer">
		/// The buffer to write signed 16-bit integer LPCM samples into. The length will be rounded down to a multiple
		/// of <see cref="ChannelCount"/>, if needed.
		/// </param>
		/// <returns>The total number of audio <c>(frames, samples)</c> read.</returns>
		public (uint Frames, uint Samples) Decode(Stream stream, Span<short> buffer)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			
			buffer = buffer.Slice(0, buffer.Length - (int)(buffer.Length % ChannelCount));
			uint fcount = (uint)buffer.Length / ChannelCount;

			// Read buffer first
			uint read = readBuffer(buffer.AsBytesUnsafe(), fcount, false);
			if (read == fcount)
				return (fcount, fcount * ChannelCount);
			buffer = buffer.Slice((int)(read * ChannelCount));

			// Decode
			read += DecodeStream(stream, buffer.AsBytesUnsafe(), fcount - read, false);
			return (read, read * ChannelCount);
		}

		/// <summary>
		/// Decodes samples from the stream into the buffer. The stream should already be at the start of the audio
		/// data, any frames or containers from the stream format should already be parsed.
		/// </summary>
		/// <param name="stream">The stream to decode samples from.</param>
		/// <param name="buffer">
		/// The buffer to write signed 32-bit floating point LPCM samples into. The length will be rounded down to a 
		/// multiple of <see cref="ChannelCount"/>, if needed.
		/// </param>
		/// <returns>The total number of audio <c>(frames, samples)</c> read.</returns>
		public (uint Frames, uint Samples) Decode(Stream stream, Span<float> buffer)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			buffer = buffer.Slice(0, buffer.Length - (int)(buffer.Length % ChannelCount));
			uint fcount = (uint)buffer.Length / ChannelCount;

			// Read buffer first
			uint read = readBuffer(buffer.AsBytesUnsafe(), fcount, true);
			if (read == fcount)
				return (fcount, fcount * ChannelCount);
			buffer = buffer.Slice((int)(read * ChannelCount));

			// Decode
			read += DecodeStream(stream, buffer.AsBytesUnsafe(), fcount - read, true);
			return (read, read * ChannelCount);
		}

		// Reads remaining frames into the buffer, returns the number of frames read
		private uint readBuffer(Span<byte> buffer, uint frameCount, bool isFloat)
		{
			uint toRead = Math.Min(BufferRemaining, frameCount);
			if (toRead == 0)
				return 0;

			if (BufferIsFloat)
			{
				var src = FloatBuffer.Slice((int)(BufferOffset * ChannelCount), (int)(toRead * ChannelCount));
				if (isFloat) // float -> float
					src.CopyTo(buffer.UnsafeCast<float>());
				else // float -> short
					SampleUtils.Convert(src, buffer.UnsafeCast<short>());
			}
			else
			{
				var src = ShortBuffer.Slice((int)(BufferOffset * ChannelCount), (int)(toRead * ChannelCount));
				if (!isFloat) // short -> short
					src.CopyTo(buffer.UnsafeCast<short>());
				else // short -> float
					SampleUtils.Convert(src, buffer.UnsafeCast<float>());
			}

			BufferOffset += toRead;
			return toRead;
		}

		/// <summary>
		/// Performs the reading of the stream to decode the data into interleaved linear pcm samples. Samples that are
		/// decoded, but not placed into <paramref name="buffer"/>, should be written to <see cref="Buffer"/>, and an
		/// appropriate call to <see cref="SetBufferCount"/> should be made.
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		/// <param name="buffer">
		/// The buffer to write decoded samples into. Buffer is guarenteed to be large enough to fit
		/// <paramref name="frameCount"/> frames, taking into account <paramref name="isFloat"/>.
		/// </param>
		/// <param name="frameCount">The number of audio frames to read.</param>
		/// <param name="isFloat">
		/// If <paramref name="buffer"/> is expecting the samples to be floats, <c>false</c> implies shorts.
		/// </param>
		/// <returns>The total number of audio frames read from the stream into <paramref name="buffer"/>.</returns>
		protected abstract uint DecodeStream(Stream stream, Span<byte> buffer, uint frameCount, bool isFloat);
		#endregion // Decode

		#region IDisposable
		public void Dispose()
		{
			if (!IsDisposed)
			{
				OnDispose(true);
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
