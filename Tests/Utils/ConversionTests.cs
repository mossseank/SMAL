/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using Xunit;
using SMAL;

namespace Tests
{
	// Performs testing for sample conversion functions
	public class ConversionTests
	{
		private const int SAMPLE_COUNT = 2 << 16;
		private static readonly short[] SHORT_SAMPLES = new short[SAMPLE_COUNT];
		private static readonly float[] FLOAT_SAMPLES = new float[SAMPLE_COUNT];
		private static readonly string AVX_FLAG_NAME = "_AllowAVX";
		private static readonly string SSE_FLAG_NAME = "_AllowSSE";
		private static readonly FieldInfo _AvxField;
		private static readonly FieldInfo _SseField;

		static ConversionTests()
		{
			// Make the test samples
			var rand = new Random();
			for (int i = 0; i < SAMPLE_COUNT; ++i)
			{
				SHORT_SAMPLES[i] = (short)rand.Next(Int16.MinValue, Int16.MaxValue);
				FLOAT_SAMPLES[i] = ((float)rand.NextDouble() * 2f) - 1f;
			}

			// Get the conversion fields
			var utilType = typeof(SampleUtils);
			_AvxField = utilType.GetField(AVX_FLAG_NAME, BindingFlags.Static | BindingFlags.NonPublic);
			_SseField = utilType.GetField(SSE_FLAG_NAME, BindingFlags.Static | BindingFlags.NonPublic);
		}

		[Fact]
		public void ShortToFloat_Test()
		{
			// Create temp float buffer
			var conv = new float[SHORT_SAMPLES.Length];
			var ret = new short[SHORT_SAMPLES.Length];

			// Perform the conversion using AVX
			if (Avx2.IsSupported)
			{
				// Allow AVX
				_AvxField.SetValue(null, true);

				SampleUtils.Convert(SHORT_SAMPLES, conv);
				SampleUtils.Convert(conv, ret);

				var badIdx = SampleCheck.FindDivergentIndex(SHORT_SAMPLES, ret);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue, 
					$"AVX short->float failed at {bi} (e: {SHORT_SAMPLES[bi]}, a: {ret[bi]})");
			}
			else Console.WriteLine("Conversion.ShortToFloat - skipping AVX test.");

			// Perform the conversion using SSE
			if (Sse2.IsSupported)
			{
				// Force SSE
				_AvxField.SetValue(null, false);
				_SseField.SetValue(null, true);

				SampleUtils.Convert(SHORT_SAMPLES, conv);
				SampleUtils.Convert(conv, ret);

				var badIdx = SampleCheck.FindDivergentIndex(SHORT_SAMPLES, ret);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"SSE short->float failed at {bi} (e: {SHORT_SAMPLES[bi]}, a: {ret[bi]})");
			}
			else Console.WriteLine("Conversion.ShortToFloat - skipping SSE test.");

			// Perform the conversion using fallback
			{
				// Force fallback
				_AvxField.SetValue(null, false);
				_SseField.SetValue(null, false);

				SampleUtils.Convert(SHORT_SAMPLES, conv);
				SampleUtils.Convert(conv, ret);

				var badIdx = SampleCheck.FindDivergentIndex(SHORT_SAMPLES, ret);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"Fallback short->float failed at {bi} (e: {SHORT_SAMPLES[bi]}, a: {ret[bi]})");
			}
		}

		[Fact]
		public void FloatToShort_Test()
		{
			// Create temp float buffer
			var conv = new short[FLOAT_SAMPLES.Length];
			var ret = new float[FLOAT_SAMPLES.Length];

			// Perform the conversion using AVX
			if (Avx2.IsSupported)
			{
				// Allow AVX
				_AvxField.SetValue(null, true);

				SampleUtils.Convert(FLOAT_SAMPLES, conv);
				SampleUtils.Convert(conv, ret);

				var badIdx = SampleCheck.FindDivergentIndex(FLOAT_SAMPLES, ret);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"AVX float->short failed at {bi} (e: {FLOAT_SAMPLES[bi]}, a: {ret[bi]})");
			}
			else Console.WriteLine("Conversion.FloatToShort - skipping AVX test.");

			// Perform the conversion using SSE
			if (Sse2.IsSupported)
			{
				// Force SSE
				_AvxField.SetValue(null, false);
				_SseField.SetValue(null, true);

				SampleUtils.Convert(FLOAT_SAMPLES, conv);
				SampleUtils.Convert(conv, ret);

				var badIdx = SampleCheck.FindDivergentIndex(FLOAT_SAMPLES, ret);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"SSE float->short failed at {bi} (e: {FLOAT_SAMPLES[bi]}, a: {ret[bi]})");
			}
			else Console.WriteLine("Conversion.FloatToShort - skipping SSE test.");

			// Perform the conversion using fallback
			{
				// Force fallback
				_AvxField.SetValue(null, false);
				_SseField.SetValue(null, false);

				SampleUtils.Convert(FLOAT_SAMPLES, conv);
				SampleUtils.Convert(conv, ret);

				var badIdx = SampleCheck.FindDivergentIndex(FLOAT_SAMPLES, ret);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"Fallback float->short failed at {bi} (e: {FLOAT_SAMPLES[bi]}, a: {ret[bi]})");
			}
		}
	}
}
