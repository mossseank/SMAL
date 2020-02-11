/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SMAL.RLAD
{
	/// <summary>
	/// Specialization of <see cref="AudioDecoder"/> that can decode both lossy and lossless RLAD (Run-Length
	/// Accumulating Deltas) audio data.
	/// </summary>
	public sealed class RladDecoder : AudioDecoder
	{
		private const uint CHANNEL_LENGTH = 1024; // 512 samples of 2-byte data
		private const uint CHUNK_SIZE = 8; // 8 samples per chunk

		#region Fields
		/// <inheritdoc/>
		public override AudioEncoding Encoding => Lossless ? AudioEncoding.RLAD : AudioEncoding.RLADLossy;
		/// <inheritdoc/>
		public override AudioChannels Channels { get; }

		/// <summary>
		/// If this decoder is decoding lossless data, <c>false</c> implies lossy data.
		/// </summary>
		public readonly bool Lossless;

		/// <summary>
		/// The header that describes the current block to decode. Must be set correctly before calling any of the
		/// <c>Decode</c> functions.
		/// </summary>
		public BlockHeader? BlockHeader;
		#endregion // Fields

		/// <summary>
		/// Initializes a decoder that can decode either lossy or lossless RLAD audio data.
		/// </summary>
		/// <param name="lossless">If the RLAD data is lossless, <c>false</c> implies lossy.</param>
		/// <param name="channels">The channel layout of the data to decode.</param>
		public RladDecoder(bool lossless, AudioChannels channels) :
			base(CHANNEL_LENGTH * (uint)channels)
		{
			Lossless = lossless;
			Channels = channels;
			BlockHeader = null;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		protected unsafe override uint Decode(Span<byte> src, Span<byte> dst, uint frameCount, bool isFloat)
		{
			if (!BlockHeader.HasValue)
				throw new InvalidOperationException("No block header provided for RLAD decoder");
			if (src.Length < BlockHeader.Value.DataSize)
				throw new IncompleteDataException("RLAD data decode", BlockHeader.Value.DataSize - (uint)src.Length);

			fixed (short* writePtr = isFloat ? ShortBuffer : dst.UnsafeCast<short>())
			fixed (byte* readPtr = src)
			{
				byte* srcPtr = readPtr;
				uint stride = ChannelCount;

				// Decode one channel at a time
				for (byte chan = 0; chan < ChannelCount; ++chan)
				{
					short* dstPtr = writePtr + chan - stride; // start -stride to prevent off-by-one

					var runs = BlockHeader.Value.GetChannelHeaders(chan);
					short sum = BlockHeader.Value.GetChannelSeed(chan);
					foreach (var run in runs)
					{
						int bps = run.Type switch { 
							0 => Lossless ? 4 : 2,
							1 => Lossless ? 8 : 4,
							2 => Lossless ? 12 : 8,
							3 => Lossless ? 16 : 12,
							_ => throw new Exception("Never reached")
						};

						if (bps == 2) // Tiny Lossy
						{
							for (int ch = 0; ch < run.Count; ++ch)
							{
								ushort p0 = *((ushort*)srcPtr + ch);

								*(dstPtr += stride) = (sum += (short)(((p0 >>  0) & 0x3) - 2));
								*(dstPtr += stride) = (sum += (short)(((p0 >>  2) & 0x3) - 2));
								*(dstPtr += stride) = (sum += (short)(((p0 >>  4) & 0x3) - 2));
								*(dstPtr += stride) = (sum += (short)(((p0 >>  6) & 0x3) - 2));
								*(dstPtr += stride) = (sum += (short)(((p0 >>  8) & 0x3) - 2));
								*(dstPtr += stride) = (sum += (short)(((p0 >> 10) & 0x3) - 2));
								*(dstPtr += stride) = (sum += (short)(((p0 >> 12) & 0x3) - 2));
								*(dstPtr += stride) = (sum += (short)(((p0 >> 14) & 0x3) - 2));
							}
						}
						else if (bps == 4) // Tiny Lossless or Small Lossy
						{
							for (int ch = 0; ch < run.Count; ++ch)
							{
								uint p0 = *((uint*)srcPtr + ch);

								*(dstPtr += stride) = (sum += (short)((int)((p0 >>  0) & 0xF) - 8));
								*(dstPtr += stride) = (sum += (short)((int)((p0 >>  4) & 0xF) - 8));
								*(dstPtr += stride) = (sum += (short)((int)((p0 >>  8) & 0xF) - 8));
								*(dstPtr += stride) = (sum += (short)((int)((p0 >> 12) & 0xF) - 8));
								*(dstPtr += stride) = (sum += (short)((int)((p0 >> 16) & 0xF) - 8));
								*(dstPtr += stride) = (sum += (short)((int)((p0 >> 20) & 0xF) - 8));
								*(dstPtr += stride) = (sum += (short)((int)((p0 >> 24) & 0xF) - 8));
								*(dstPtr += stride) = (sum += (short)((int)((p0 >> 28) & 0xF) - 8));
							}
						}
						else if (bps == 8) // Small Lossless or Medium Lossy
						{
							sbyte* sbtSrc = (sbyte*)srcPtr;

							for (int ch = 0; ch < run.Count; ++ch, sbtSrc += 8)
							{
								*(dstPtr += stride) = (sum += sbtSrc[0]);
								*(dstPtr += stride) = (sum += sbtSrc[1]);
								*(dstPtr += stride) = (sum += sbtSrc[2]);
								*(dstPtr += stride) = (sum += sbtSrc[3]);
								*(dstPtr += stride) = (sum += sbtSrc[4]);
								*(dstPtr += stride) = (sum += sbtSrc[5]);
								*(dstPtr += stride) = (sum += sbtSrc[6]);
								*(dstPtr += stride) = (sum += sbtSrc[7]);
							}
						}
						else if (bps == 12) // Medium Lossless or Full Lossy
						{
							uint* intPtr = (uint*)srcPtr;

							for (int ch = 0; ch < run.Count; ++ch, intPtr += 3)
							{
								uint p0 = intPtr[0];
								uint p1 = intPtr[1];
								uint p2 = intPtr[2];

								*(dstPtr += stride) = (sum += (short)((int)(( p0 >>  0              ) & 0xFFF) - 2048));
								*(dstPtr += stride) = (sum += (short)((int)(( p0 >> 12              ) & 0xFFF) - 2048));
								*(dstPtr += stride) = (sum += (short)((int)(((p0 >> 24) | (p1 <<  8)) & 0xFFF) - 2048));
								*(dstPtr += stride) = (sum += (short)((int)(( p1 >>  4              ) & 0xFFF) - 2048));
								*(dstPtr += stride) = (sum += (short)((int)(( p1 >> 16              ) & 0xFFF) - 2048));
								*(dstPtr += stride) = (sum += (short)((int)(((p1 >> 28) | (p2 <<  4)) & 0xFFF) - 2048));
								*(dstPtr += stride) = (sum += (short)((int)(( p2 >>  8              ) & 0xFFF) - 2048));
								*(dstPtr += stride) = (sum += (short)((int)(( p2 >> 20              ) & 0xFFF) - 2048));
							}
						}
						else // Full Lossless (16 bps)
						{
							short* srtSrc = (short*)srcPtr;

							for (int ch = 0; ch < run.Count; ++ch, srtSrc += 8)
							{
								*(dstPtr += stride) = (sum += srtSrc[0]);
								*(dstPtr += stride) = (sum += srtSrc[1]);
								*(dstPtr += stride) = (sum += srtSrc[2]);
								*(dstPtr += stride) = (sum += srtSrc[3]);
								*(dstPtr += stride) = (sum += srtSrc[4]);
								*(dstPtr += stride) = (sum += srtSrc[5]);
								*(dstPtr += stride) = (sum += srtSrc[6]);
								*(dstPtr += stride) = (sum += srtSrc[7]);
							}
						}

						srcPtr += (uint)(bps * run.Count); // Move down the source data
					}
				}
			}

			// For lossy data, need to convert [-2048,2047] -> [-32768,32767] (mult 16, lshift 4)
			if (!Lossless)
			{
				uint total = ChannelCount * 512;
				uint count = 0;

				fixed (short* sampPtr = isFloat ? ShortBuffer : dst.UnsafeCast<short>())
				{
					if (Avx2.IsSupported)
					{
						while ((count + 16) <= total)
						{
							Vector256<short>* samp = (Vector256<short>*)(sampPtr + count);
							Avx.Store(sampPtr + count, Avx2.ShiftLeftLogical(*samp, 4));
							count += 16;
						}
					}
					else if (Sse2.IsSupported)
					{
						while ((count + 8) <= total)
						{
							Vector128<short>* samp = (Vector128<short>*)(sampPtr + count);
							Sse2.Store(sampPtr + count, Sse2.ShiftLeftLogical(*samp, 4));
							count += 8;
						}
					}
					else // Loop fallback - TODO: Arm intrinsics
					{
						while (count < total)
						{
							sampPtr[count++] <<= 4;
							sampPtr[count++] <<= 4;
							sampPtr[count++] <<= 4;
							sampPtr[count++] <<= 4;
							sampPtr[count++] <<= 4;
							sampPtr[count++] <<= 4;
							sampPtr[count++] <<= 4;
							sampPtr[count++] <<= 4;
						}
					}
				}
			}

			// Convert to float if needed
			if (isFloat)
				SampleUtils.Convert(ShortBuffer, dst.UnsafeCast<float>());

			return frameCount;
		}

		protected override void OnDispose(bool disposing) { }
	}
}
