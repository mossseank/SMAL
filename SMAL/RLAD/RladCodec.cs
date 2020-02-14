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
	/// Specialization of <see cref="AudioCodec"/> that can decode both lossy and lossless RLAD (Run-Length
	/// Accumulating Deltas) audio data.
	/// </summary>
	public sealed class RladCodec : AudioCodec
	{
		private const uint CHANNEL_LENGTH = 1024; // 512 samples of 2-byte data
		private const uint CHUNK_SIZE = 8; // 8 samples per chunk
		private const short BPS_12_MAX =  2047;
		private const short BPS_12_MIN = -2048;
		private const short BPS_08_MAX =  127;
		private const short BPS_08_MIN = -128;
		private const short BPS_04_MAX =  7;
		private const short BPS_04_MIN = -8;
		private const short BPS_02_MAX =  1;
		private const short BPS_02_MIN = -2;

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
		public RladCodec(bool lossless, AudioChannels channels) :
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
					short sum = 0;
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
							for (int ch = 0; ch < run.Extra; ++ch)
							{
								short p0 = *((short*)srcPtr + ch);

								*(dstPtr += stride) = sum += (short)((p0 << 14) >> 14);
								*(dstPtr += stride) = sum += (short)((p0 << 12) >> 14);
								*(dstPtr += stride) = sum += (short)((p0 << 10) >> 14);
								*(dstPtr += stride) = sum += (short)((p0 <<  8) >> 14);
								*(dstPtr += stride) = sum += (short)((p0 <<  6) >> 14);
								*(dstPtr += stride) = sum += (short)((p0 <<  4) >> 14);
								*(dstPtr += stride) = sum += (short)((p0 <<  2) >> 14);
								*(dstPtr += stride) = sum += (short)((p0 <<  0) >> 14);
							}
						}
						else if (bps == 4) // Tiny Lossless or Small Lossy
						{
							for (int ch = 0; ch < run.Extra; ++ch)
							{
								int p0 = *((int*)srcPtr + ch);

								*(dstPtr += stride) = sum += (short)((p0 << 28) >> 28);
								*(dstPtr += stride) = sum += (short)((p0 << 24) >> 28);
								*(dstPtr += stride) = sum += (short)((p0 << 20) >> 28);
								*(dstPtr += stride) = sum += (short)((p0 << 16) >> 28);
								*(dstPtr += stride) = sum += (short)((p0 << 12) >> 28);
								*(dstPtr += stride) = sum += (short)((p0 <<  8) >> 28);
								*(dstPtr += stride) = sum += (short)((p0 <<  4) >> 28);
								*(dstPtr += stride) = sum += (short)((p0 <<  0) >> 28);
							}
						}
						else if (bps == 8) // Small Lossless or Medium Lossy
						{
							sbyte* sbtSrc = (sbyte*)srcPtr;

							for (int ch = 0; ch < run.Extra; ++ch, sbtSrc += 8)
							{
								*(dstPtr += stride) = sum += sbtSrc[0];
								*(dstPtr += stride) = sum += sbtSrc[1];
								*(dstPtr += stride) = sum += sbtSrc[2];
								*(dstPtr += stride) = sum += sbtSrc[3];
								*(dstPtr += stride) = sum += sbtSrc[4];
								*(dstPtr += stride) = sum += sbtSrc[5];
								*(dstPtr += stride) = sum += sbtSrc[6];
								*(dstPtr += stride) = sum += sbtSrc[7];
							}
						}
						else if (bps == 12) // Medium Lossless or Full Lossy
						{
							int* intPtr = (int*)srcPtr;

							for (int ch = 0; ch < run.Extra; ++ch, intPtr += 3)
							{
								long p0 = *(long*)intPtr;
								int p1 = intPtr[2];

								*(dstPtr += stride) = sum += (short)( (p0 << 52) >> 52);
								*(dstPtr += stride) = sum += (short)( (p0 << 40) >> 52);
								*(dstPtr += stride) = sum += (short)( (p0 << 28) >> 52);
								*(dstPtr += stride) = sum += (short)( (p0 << 16) >> 52);
								*(dstPtr += stride) = sum += (short)( (p0 <<  4) >> 52);
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
								*(dstPtr += stride) = sum += (short)(((p0 >> 60) & 0xF) | ((p1 << 24) >> 20));
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
								*(dstPtr += stride) = sum += (short)( (p1 << 12) >> 20);
								*(dstPtr += stride) = sum += (short)( (p1 <<  0) >> 20);
							}
						}
						else // Full Lossless (16 bps)
						{
							short* srtSrc = (short*)srcPtr;

							for (int ch = 0; ch < run.Extra; ++ch, srtSrc += 8)
							{
								*(dstPtr += stride) = sum += srtSrc[0];
								*(dstPtr += stride) = sum += srtSrc[1];
								*(dstPtr += stride) = sum += srtSrc[2];
								*(dstPtr += stride) = sum += srtSrc[3];
								*(dstPtr += stride) = sum += srtSrc[4];
								*(dstPtr += stride) = sum += srtSrc[5];
								*(dstPtr += stride) = sum += srtSrc[6];
								*(dstPtr += stride) = sum += srtSrc[7];
							}
						}

						srcPtr += (uint)(bps * run.Extra); // Move down the source data
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

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		protected unsafe override uint Encode(Span<byte> src, Span<byte> dst, uint frameCount, bool isFloat)
		{
			if (frameCount != 512)
				throw new InvalidOperationException($"RLAD encoding must have exactly 512 samples ({frameCount} given)");

			// Convert to shorts if needed
			if (isFloat)
				SampleUtils.Convert(src.UnsafeCast<float>(), ShortBuffer);

			// If lossy, convert [-32768,32767] -> [-2048,2047] (div 16, rshift 4)
			if (!Lossless)
			{
				uint total = ChannelCount * 512;
				uint count = 0;

				fixed (short* sampPtr = isFloat ? ShortBuffer : src.UnsafeCast<short>())
				{
					if (Avx2.IsSupported)
					{
						while ((count + 16) <= total)
						{
							Vector256<short>* samp = (Vector256<short>*)(sampPtr + count);
							Avx.Store(sampPtr + count, Avx2.ShiftRightArithmetic(*samp, 4));
							count += 16;
						}
					}
					else if (Sse2.IsSupported)
					{
						while ((count + 8) <= total)
						{
							Vector128<short>* samp = (Vector128<short>*)(sampPtr + count);
							Sse2.Store(sampPtr + count, Sse2.ShiftRightArithmetic(*samp, 4));
							count += 8;
						}
					}
					else // Loop fallback - TODO: Arm intrinsics
					{
						while (count < total)
						{
							sampPtr[count++] >>= 4;
							sampPtr[count++] >>= 4;
							sampPtr[count++] >>= 4;
							sampPtr[count++] >>= 4;
							sampPtr[count++] >>= 4;
							sampPtr[count++] >>= 4;
							sampPtr[count++] >>= 4;
							sampPtr[count++] >>= 4;
						}
					}
				}
			}

			// Convert the samples to deltas
			BlockHeader header = default;
			ConvertToDeltas((isFloat || !Lossless) ? ShortBuffer : src.UnsafeCast<short>(), 
				ShortBuffer, ChannelCount, Lossless, ref header);

			// Write the actual data
			int stride = (int)ChannelCount;
			fixed (byte* writePtr = dst)
			fixed (short* readPtr = ShortBuffer)
			{
				var dstPtr = writePtr;

				for (byte chan = 0; chan < ChannelCount; ++chan)
				{
					var srcPtr = readPtr + chan - stride; // start -stride to prevent off-by-one
					var runs = header.GetChannelHeaders(chan, true);

					foreach (var run in runs)
					{
						int bps = run.Type switch {
							0 => Lossless ? 4 : 2,
							1 => Lossless ? 8 : 4,
							2 => Lossless ? 12 : 8,
							3 => Lossless ? 16 : 12,
							_ => throw new Exception("Never reached")
						};

						if (bps == 2)
						{
							int packed =
								((*(srcPtr += stride) <<  0) & 0x0003) |
								((*(srcPtr += stride) <<  2) & 0x000C) |
								((*(srcPtr += stride) <<  4) & 0x0030) |
								((*(srcPtr += stride) <<  6) & 0x00C0) |
								((*(srcPtr += stride) <<  8) & 0x0300) |
								((*(srcPtr += stride) << 10) & 0x0C00) |
								((*(srcPtr += stride) << 12) & 0x3000) |
								((*(srcPtr += stride) << 14) & 0xC000) ;
							*(ushort*)dstPtr = (ushort)packed;
						}
						else if (bps == 4)
						{
							long packed =
								((*(srcPtr += stride) <<  0) & 0x0000000FL) |
								((*(srcPtr += stride) <<  4) & 0x000000F0L) |
								((*(srcPtr += stride) <<  8) & 0x00000F00L) |
								((*(srcPtr += stride) << 12) & 0x0000F000L) |
								((*(srcPtr += stride) << 16) & 0x000F0000L) |
								((*(srcPtr += stride) << 20) & 0x00F00000L) |
								((*(srcPtr += stride) << 24) & 0x0F000000L) |
								((*(srcPtr += stride) << 28) & 0xF0000000L) ;
							*(uint*)dstPtr = (uint)packed;
						}
						else if (bps == 8)
						{
							long packed1 =
								((*(srcPtr += stride) <<  0) & 0x000000FFL) |
								((*(srcPtr += stride) <<  8) & 0x0000FF00L) |
								((*(srcPtr += stride) << 16) & 0x00FF0000L) |
								((*(srcPtr += stride) << 24) & 0xFF000000L) ;
							long packed2 =
								((*(srcPtr += stride) <<  0) & 0x000000FFL) |
								((*(srcPtr += stride) <<  8) & 0x0000FF00L) |
								((*(srcPtr += stride) << 16) & 0x00FF0000L) |
								((*(srcPtr += stride) << 24) & 0xFF000000L) ;
							*(uint*)dstPtr       = (uint)packed1;
							*(uint*)(dstPtr + 4) = (uint)packed2;

						}
						else if (bps == 12)
						{
							long packed1 =
								((*(srcPtr += stride) <<  0) & 0x00000FFFL) |
								((*(srcPtr += stride) << 12) & 0x00FFF000L) |
								((*(srcPtr += stride) << 24) & 0xFF000000L) ;
							long packed2 =
								((*(srcPtr          ) >>  8) & 0x0000000FL) |
								((*(srcPtr += stride) <<  4) & 0x0000FFF0L) |
								((*(srcPtr += stride) << 16) & 0x0FFF0000L) |
								((*(srcPtr += stride) << 28) & 0xF0000000L) ;
							long packed3 =
								((*(srcPtr          ) >>  4) & 0x000000FFL) |
								((*(srcPtr += stride) <<  8) & 0x000FFF00L) |
								((*(srcPtr += stride) << 20) & 0xFFF00000L) ;
							*(uint*)dstPtr       = (uint)packed1;
							*(uint*)(dstPtr + 4) = (uint)packed2;
							*(uint*)(dstPtr + 8) = (uint)packed3;
						}
						else
						{
							*(short*)(dstPtr +  0) = *(srcPtr += stride);
							*(short*)(dstPtr +  2) = *(srcPtr += stride);
							*(short*)(dstPtr +  4) = *(srcPtr += stride);
							*(short*)(dstPtr +  6) = *(srcPtr += stride);
							*(short*)(dstPtr +  8) = *(srcPtr += stride);
							*(short*)(dstPtr + 10) = *(srcPtr += stride);
							*(short*)(dstPtr + 12) = *(srcPtr += stride);
							*(short*)(dstPtr + 14) = *(srcPtr += stride);
						}

						dstPtr += bps;
					}
				}

				header.DataSize = (ushort)(dstPtr - writePtr);
			}

			// Create the final run sets before returning
			CompressRuns(ref header, ChannelCount);
			BlockHeader = header;

			return frameCount;
		}

		// Converts the samples into their deltas, and calculates a run type for each chunk
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe static void ConvertToDeltas(Span<short> src, Span<short> dst, uint channels, bool lossless, 
			ref BlockHeader head)
		{
			uint 
				off0 = channels * 0, off1 = channels * 1, off2 = channels * 2, off3 = channels * 3,
				off4 = channels * 4, off5 = channels * 5, off6 = channels * 6, off7 = channels * 7;
			uint stride = channels * 8;
			short
				fMax = lossless ? BPS_12_MAX : BPS_08_MAX,
				fMin = lossless ? BPS_12_MIN : BPS_08_MIN,
				mMax = lossless ? BPS_08_MAX : BPS_04_MAX,
				mMin = lossless ? BPS_08_MIN : BPS_04_MIN,
				sMax = lossless ? BPS_04_MAX : BPS_02_MAX,
				sMin = lossless ? BPS_04_MIN : BPS_02_MIN;

			fixed (short* srcPtr = src, dstPtr = dst)
			{
				for (uint ch = 0; ch < channels; ++ch)
				{
					uint total = 0;
					short last = 0;
					short* srcBase = srcPtr + ch, dstBase = dstPtr + ch;
					var runs = head.GetChannelHeaders((byte)ch, true);
					int ridx = 0;

					while (total < 512)
					{
						short end = srcBase[off7];

						// TODO - this can be done with intrinsics
						short d0 = dstBase[off0] = (short)(srcBase[off0] - last);
						short d1 = dstBase[off1] = (short)(srcBase[off1] - srcBase[off0]);
						short d2 = dstBase[off2] = (short)(srcBase[off2] - srcBase[off1]);
						short d3 = dstBase[off3] = (short)(srcBase[off3] - srcBase[off2]);
						short d4 = dstBase[off4] = (short)(srcBase[off4] - srcBase[off3]);
						short d5 = dstBase[off5] = (short)(srcBase[off5] - srcBase[off4]);
						short d6 = dstBase[off6] = (short)(srcBase[off6] - srcBase[off5]);
						short d7 = dstBase[off7] = (short)(srcBase[off7] - srcBase[off6]);

						bool	
							full  = (d0 < fMin) || (d0 > fMax) ||
								    (d1 < fMin) || (d1 > fMax) ||
								    (d2 < fMin) || (d2 > fMax) ||
								    (d3 < fMin) || (d3 > fMax) ||
								    (d4 < fMin) || (d4 > fMax) ||
								    (d5 < fMin) || (d5 > fMax) ||
								    (d6 < fMin) || (d6 > fMax) ||
								    (d7 < fMin) || (d7 > fMax)   ,
							med   = !full && (
								    (d0 < mMin) || (d0 > mMax) ||
								    (d1 < mMin) || (d1 > mMax) ||
								    (d2 < mMin) || (d2 > mMax) ||
								    (d3 < mMin) || (d3 > mMax) ||
								    (d4 < mMin) || (d4 > mMax) ||
								    (d5 < mMin) || (d5 > mMax) ||
								    (d6 < mMin) || (d6 > mMax) ||
								    (d7 < mMin) || (d7 > mMax) ) ,
							small = !full && !med && (
								    (d0 < sMin) || (d0 > sMax) ||
								    (d1 < sMin) || (d1 > sMax) ||
								    (d2 < sMin) || (d2 > sMax) ||
								    (d3 < sMin) || (d3 > sMax) ||
								    (d4 < sMin) || (d4 > sMax) ||
								    (d5 < sMin) || (d5 > sMax) ||
								    (d6 < sMin) || (d6 > sMax) ||
								    (d7 < sMin) || (d7 > sMax) );

						runs[ridx++] = new RunHeader(full ? 3 : med ? 2 : small ? 1 : 0, 0);
						last = end;
						total += 8;
						srcBase += stride;
						dstBase += stride;
					}
				}
			}
		}

		// Compresses the runs into runs-of-runs of the same type
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe static void CompressRuns(ref BlockHeader header, uint channels)
		{
			for (uint ch = 0; ch < channels; ++ch)
			{
				var runs = header.GetChannelHeaders((byte)ch, true);
				int readOff = 0;
				int writeOff = 0;

				while (readOff < 64)
				{
					int ctype = runs[readOff].Type;
					int extra = 0;

					// Scan to find first of different type
					while ((++readOff < 64) && (runs[readOff].Type == ctype))
						extra += 1;

					runs[writeOff++] = new RunHeader(ctype, extra);
				}

				header.SetChannelCount((byte)ch, (byte)writeOff);
			}
		}

		protected override void OnDispose(bool disposing) { }
	}
}
