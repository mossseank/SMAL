/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using System.IO;
using System.Runtime.CompilerServices;

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

		private byte[] _buffer; // Internal general-use buffer for decoding operations
		#endregion // Buffer

		/// <summary>
		/// If this decoder has been disposed.
		/// </summary>
		protected bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Initializes the components of the base decoder type.
		/// </summary>
		/// <param name="bufferSize">The initial size of the internal general-use buffer.</param>
		protected AudioDecoder(uint bufferSize)
		{
			_buffer = new byte[bufferSize];
		}
		~AudioDecoder()
		{
			if (!IsDisposed)
				OnDispose(false);
			IsDisposed = true;
		}

		/// <summary>
		/// Ensures that the internal buffer is at least the given number of bytes.
		/// </summary>
		/// <param name="size">The minimum size of the buffer, in bytes.</param>
		/// <param name="keep">If the existing data in the buffer should be kept.</param>
		/// <returns>If the buffer was resized.</returns>
		protected unsafe bool EnsureBufferSize(uint size, bool keep = false)
		{
			if ((uint)_buffer.LongLength < size)
			{
				var nb = new byte[size];
				if (keep)
				{
					fixed (byte* dst = nb, src = _buffer)
						Unsafe.CopyBlock(dst, src, size);
				}
				_buffer = nb;
				return true;
			}
			return false;
		}

		#region Decode
		/// <summary>
		/// Decodes the data from the source span into the destination span. The destination span will be rounded down
		/// to a multiple of <see cref="ChannelCount"/>, if needed.
		/// </summary>
		/// <remarks>
		/// The source should only contain the raw audio data in the expected format - headers and frame-based formats 
		/// should have already parsed this information and made it available for use.
		/// </remarks>
		/// <param name="src">The raw format source data, without any header/frame data, where applicable.</param>
		/// <param name="dst">The destination for decoded 32-bit floating point interleaved LPCM samples.</param>
		/// <returns>The actual number of audio <em>frames</em> decoded.</returns>
		public uint Decode(Span<byte> src, Span<float> dst)
		{
			if (src.Length == 0)
				return 0;

			dst = dst.Slice(0, dst.Length - (int)(dst.Length % ChannelCount));
			uint fcount = (uint)dst.Length / ChannelCount;
			return Decode(src, dst.AsBytesUnsafe(), fcount, true);
		}

		/// <summary>
		/// Decodes the data from the source span into the destination span. The destination span will be rounded down
		/// to a multiple of <see cref="ChannelCount"/>, if needed.
		/// </summary>
		/// <remarks>
		/// The source should only contain the raw audio data in the expected format - headers and frame-based formats 
		/// should have already parsed this information and made it available for use.
		/// </remarks>
		/// <param name="src">The raw format source data, without any header/frame data, where applicable.</param>
		/// <param name="dst">The destination for decoded 16-bit signed integer interleaved LPCM samples.</param>
		/// <returns>The actual number of audio <em>frames</em> decoded.</returns>
		public uint Decode(Span<byte> src, Span<short> dst)
		{
			if (src.Length == 0)
				return 0;

			dst = dst.Slice(0, dst.Length - (int)(dst.Length % ChannelCount));
			uint fcount = (uint)dst.Length / ChannelCount;
			return Decode(src, dst.AsBytesUnsafe(), fcount, false);
		}

		/// <summary>
		/// Implements the decoding for the given format.
		/// </summary>
		/// <param name="src">The raw source format data.</param>
		/// <param name="dst">
		/// The destination for the decoded samples, should be written as the type implied by 
		/// <paramref name="isFloat"/>. Its size will be pre-adjusted to be a multiple of <see cref="ChannelCount"/>,
		/// and it will be big enough to accept the number of frames in <paramref name="frameCount"/>.
		/// </param>
		/// <param name="frameCount">The target number of frames to decode.</param>
		/// <param name="isFloat">If the decoded data is expected as <c>float</c>, otherwise <c>short</c>.</param>
		/// <returns>The actual number of decoded audio frames.</returns>
		protected abstract uint Decode(Span<byte> src, Span<byte> dst, uint frameCount, bool isFloat);
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
