/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SMAL
{
	/// <summary>
	/// Utilities for working with audio sample values.
	/// </summary>
	public static class SampleUtils
	{
		// Used for testing
		private static bool _AllowAVX = true;
		private static bool _AllowSSE = true;

		/// <summary>
		/// Converts a span of signed 16-bit integer samples into normalized 32-bit floating point samples.
		/// <para>
		/// This function uses hardware intrinsics on systems that support them.
		/// </para>
		/// </summary>
		/// <param name="src">The source samples to convert.</param>
		/// <param name="dst">The buffer in which to place the converted samples.</param>
		/// <returns>The total number of samples converted.</returns>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe static uint Convert(ReadOnlySpan<short> src, Span<float> dst)
		{
			uint total = (uint)((src.Length < dst.Length) ? src.Length : dst.Length);

			fixed (short* srcPtr = src)
			fixed (float* dstPtr = dst)
			{
				uint offset = 0;
				var scale32 = 1f/32_767f;

				if (Avx2.IsSupported && _AllowAVX) // x86 processors since 2013 (intel) and 2015 (amd)
				{
					var scale256 = Vector256.Create(scale32);
					while ((offset + 8) < total)
					{
						var intVec = Avx2.ConvertToVector256Int32(srcPtr + offset);
						var fltVec = Avx.ConvertToVector256Single(intVec);
						
						Avx.Store(dstPtr + offset, Avx.Multiply(fltVec, scale256));
						offset += 8;
					}
				}
				else if (Sse2.IsSupported && _AllowSSE) // All other x86 - .Net Core 3 requires SSE2
				{
					var scale128 = Vector128.Create(scale32);
					while ((offset + 8) < total)
					{
						var sv1 = (Vector128<short>*)(srcPtr + offset);
						var iv1 = Sse2.ShiftRightArithmetic(Sse2.UnpackLow(*sv1, *sv1).AsInt32(), 16);
						var iv2 = Sse2.ShiftRightArithmetic(Sse2.UnpackHigh(*sv1, *sv1).AsInt32(), 16);

						var fv1 = Sse2.ConvertToVector128Single(iv1);
						var fv2 = Sse2.ConvertToVector128Single(iv2);
						Sse.Store(dstPtr + offset, Sse.Multiply(fv1, scale128));
						Sse.Store(dstPtr + offset + 4, Sse.Multiply(fv2, scale128));
						offset += 8;
					}
				}
				else // Loop fallback - non-x86 platforms (TODO: ARM intrinsics)
				{
					while ((offset + 8) < total)
					{
						dstPtr[offset  ] = srcPtr[offset  ] * scale32;
						dstPtr[offset+1] = srcPtr[offset+1] * scale32;
						dstPtr[offset+2] = srcPtr[offset+2] * scale32;
						dstPtr[offset+3] = srcPtr[offset+3] * scale32;
						dstPtr[offset+4] = srcPtr[offset+4] * scale32;
						dstPtr[offset+5] = srcPtr[offset+5] * scale32;
						dstPtr[offset+6] = srcPtr[offset+6] * scale32;
						dstPtr[offset+7] = srcPtr[offset+7] * scale32;
						offset += 8;
					}
				}

				while (offset < total)
				{
					*(dstPtr + offset) = (*(srcPtr + offset) * scale32);
					offset += 1;
				}
			}

			return total;
		}

		/// <summary>
		/// Converts a span of normalized 32-bit floating point samples into signed 16-bit integer samples.
		/// <para>
		/// This function uses hardware intrinsics on systems that support them.
		/// </para>
		/// </summary>
		/// <param name="src">The source samples to convert.</param>
		/// <param name="dst">The buffer in which to place the converted samples.</param>
		/// <returns>The total number of samples converted.</returns>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public unsafe static uint Convert(ReadOnlySpan<float> src, Span<short> dst)
		{
			uint total = (uint)((src.Length < dst.Length) ? src.Length : dst.Length);

			fixed (float* srcPtr = src)
			fixed (short* dstPtr = dst)
			{
				uint offset = 0;
				var scale32 = 32_767f;

				if (Avx.IsSupported && _AllowAVX) // x86 processors since 2011
				{
					var scale256 = Vector256.Create(scale32);
					while ((offset + 8) < total)
					{
						var fltVec = Avx.Multiply(*(Vector256<float>*)(srcPtr + offset), scale256);
						var intVec = Avx.ConvertToVector256Int32(fltVec);

						var srtVec = Sse2.PackSignedSaturate(intVec.GetLower(), intVec.GetUpper());
						Sse2.Store(dstPtr + offset, srtVec);
						offset += 8;
					}
				}
				else if (Sse2.IsSupported && _AllowSSE) // All other x86 - .Net Core 3 requires SSE2
				{
					var scale128 = Vector128.Create(scale32);
					while ((offset + 8) < total)
					{
						var fv1 = Sse.Multiply(*(Vector128<float>*)(srcPtr + offset    ), scale128);
						var fv2 = Sse.Multiply(*(Vector128<float>*)(srcPtr + offset + 4), scale128);
						var iv1 = Sse2.ConvertToVector128Int32(fv1);
						var iv2 = Sse2.ConvertToVector128Int32(fv2);

						var srtVec = Sse2.PackSignedSaturate(iv1, iv2);
						Sse2.Store(dstPtr + offset, srtVec);
						offset += 8;
					}
				}
				else // Loop fallback - non-x86 platforms (TODO: ARM intrinsics)
				{
					while ((offset + 8) < total)
					{
						dstPtr[offset  ] = (short)(srcPtr[offset  ] * scale32);
						dstPtr[offset+1] = (short)(srcPtr[offset+1] * scale32);
						dstPtr[offset+2] = (short)(srcPtr[offset+2] * scale32);
						dstPtr[offset+3] = (short)(srcPtr[offset+3] * scale32);
						dstPtr[offset+4] = (short)(srcPtr[offset+4] * scale32);
						dstPtr[offset+5] = (short)(srcPtr[offset+5] * scale32);
						dstPtr[offset+6] = (short)(srcPtr[offset+6] * scale32);
						dstPtr[offset+7] = (short)(srcPtr[offset+7] * scale32);
						offset += 8;
					}
				}

				while (offset < total)
				{
					*(dstPtr + offset) = (short)(*(srcPtr + offset) * scale32);
					offset += 1;
				}
			}

			return total;
		}
	}
}
