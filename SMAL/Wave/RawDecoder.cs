/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL.Wave
{
	/// <summary>
	/// Specialization of <see cref="AudioDecoder"/> that can decode raw audio data formats. This decoder can be used
	/// on raw PCM data, such as that found in Wave files.
	/// </summary>
	public sealed class RawDecoder : AudioDecoder
	{
		private const uint LOOP_FRAME_COUNT = 256;

		#region Fields
		/// <inheritdoc/>
		public override AudioEncoding Encoding { get; }
		/// <inheritdoc/>
		public override AudioChannels Channels { get; }

		// Sizes for source data (bytes)
		private readonly uint _sampleSize;
		private readonly uint _frameSize;
		#endregion // Fields

		/// <summary>
		/// Initializes a decoder that can decode audio data of the given format and channel count.
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

		protected override uint Decode(Span<byte> src, Span<byte> dst, uint frameCount, bool isFloat)
		{
			// Slice the source to the expected size
			uint decodeCount = Math.Min(frameCount, (uint)src.Length / _frameSize);
			src = src.Slice(0, (int)(decodeCount * _frameSize));

			// Split based on encoding type
			if (Encoding == AudioEncoding.Pcm)
			{
				if (isFloat) // short -> float
					SampleUtils.Convert(src.UnsafeCast<short>(), dst.UnsafeCast<float>());
				else // short -> short
					src.UnsafeCast<short>().CopyTo(dst.UnsafeCast<short>());
			}
			else if (Encoding == AudioEncoding.IeeeFloat)
			{
				if (isFloat) // float -> float
					src.UnsafeCast<float>().CopyTo(dst.UnsafeCast<float>());
				else // float -> short
					SampleUtils.Convert(src.UnsafeCast<float>(), dst.UnsafeCast<short>());
			}
			else
				decodeCount = 0;

			return decodeCount;
		}

		protected override void OnDispose(bool disposing) { }
	}
}
