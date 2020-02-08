/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SMAL.Wave
{
	/// <summary>
	/// Specialization of <see cref="AudioDecoder"/> that can decode raw audio data formats. This decoder can be used
	/// on raw PCM data, such as that found in Wave files.
	/// </summary>
	public sealed class RawDecoder : AudioDecoder
	{
		private const uint LOOP_FRAME_COUNT = 64;

		#region Fields
		/// <inheritdoc/>
		public override AudioEncoding Encoding { get; }
		/// <inheritdoc/>
		public override AudioChannels Channels { get; }

		// Sizes for data coming from stream
		private readonly uint _sampleSize;
		private readonly uint _frameSize;
		#endregion // Fields

		/// <summary>
		/// Creates a new decoder that can decode audio data of the given format and channel count.
		/// </summary>
		/// <param name="encoding">The encoding format to decode from. Must be a RAW format.</param>
		/// <param name="channels">The channel layout of the data to decode.</param>
		public RawDecoder(AudioEncoding encoding, AudioChannels channels) :
			base(0)
		{
			Encoding = encoding;
			Channels = channels;

			_sampleSize = encoding switch {
				AudioEncoding.Pcm => 2u,
				AudioEncoding.IeeeFloat => 4u,
				_ => throw new ArgumentException($"Invalid RAW encoding '{encoding}'", nameof(encoding))
			};
			_frameSize = _sampleSize * ChannelCount;
		}

		protected override uint DecodeStream(Stream stream, Span<byte> buffer, uint frameCount, bool isFloat)
		{
			// Buffer for XX frames at a time (of up-to-32-bit data)
			Span<byte> stage = stackalloc byte[(int)(ChannelCount * LOOP_FRAME_COUNT * 4u)];
			var srcFloat = stage.UnsafeCast<float>();
			var srcShort = stage.UnsafeCast<short>();
			var srcSByte = stage.UnsafeCast<sbyte>();

			var dstFloat = buffer.UnsafeCast<float>();
			var dstShort = buffer.UnsafeCast<short>();

			uint total = 0;   // Total number of frames read
			uint current = 0; // Number of frames for current loop iteration
			while ((total < frameCount) && 
				  ((current = readFrames(stream, stage, Math.Min(frameCount - total, LOOP_FRAME_COUNT))) > 0))
			{
				var sampCount = (int)(current * ChannelCount);

				if (Encoding == AudioEncoding.Pcm)
				{
					if (!isFloat) // Direct short -> short
						srcShort.Slice(0, sampCount).CopyTo(dstShort);
					else // Convert short -> float
						SampleUtils.Convert(srcShort.Slice(0, sampCount), dstFloat);
					dstFloat = dstFloat.Slice(sampCount);
				}
				else if (Encoding == AudioEncoding.IeeeFloat)
				{
					if (isFloat) // Direct float -> float
						srcFloat.Slice(0, sampCount).CopyTo(dstFloat);
					else // Convert float -> short
						SampleUtils.Convert(srcFloat.Slice(0, sampCount), dstShort);
					dstShort = dstShort.Slice(sampCount);
				}

				total += current;
			}

			SetBufferCount(0, false);
			return total;
		}

		// Returns the actual number of whole frames read
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private uint readFrames(Stream stream, Span<byte> buffer, uint frameCount)
		{
			var read = (uint)stream.Read(buffer.Slice(0, (int)(_frameSize * frameCount)));
			if ((read % _frameSize) != 0)
				throw new IncompleteFrameException(Encoding, Channels, read % _frameSize);
			return read / _frameSize;
		}

		protected override void OnDispose(bool disposing) { }
	}
}
