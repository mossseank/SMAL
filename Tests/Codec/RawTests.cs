/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using Xunit;
using SMAL.Wave;
using SMAL;

namespace Tests.Codec
{
	// Performs testing for RAW codec functions
	public class RawTests
	{
		private const int SAMPLE_COUNT = 4096;
		private static readonly short[] SHORT_SAMPLES = new short[SAMPLE_COUNT];
		private static readonly float[] FLOAT_SAMPLES = new float[SAMPLE_COUNT];

		static RawTests()
		{
			// Make the test samples
			var rand = new Random();
			for (int i = 0; i < SAMPLE_COUNT; ++i)
			{
				SHORT_SAMPLES[i] = (short)rand.Next(Int16.MinValue, Int16.MaxValue);
				SampleUtils.Convert(SHORT_SAMPLES, FLOAT_SAMPLES);
			}
		}

		[Fact]
		public void BadFormat_Test()
		{
			var ex = Assert.Throws<ArgumentException>(() => new RawCodec(AudioEncoding.RLAD, AudioChannels.Mono));
			Assert.True(ex.Message.StartsWith("Invalid RAW"), "Unexpected exception thrown by bad format.");
		}

		[Fact]
		public void SameFormatDecode_Test()
		{
			// Float->Float
			{
				var fraw = new RawCodec(AudioEncoding.IeeeFloat, AudioChannels.Mono);
				float[] ftmp = new float[FLOAT_SAMPLES.Length];
				fraw.Decode(FLOAT_SAMPLES.AsSpan().AsBytesUnsafe(), ftmp);
				var badIdx = SampleCheck.FindDivergentIndex(FLOAT_SAMPLES, ftmp, false);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"float->float decode failed at {bi} (e: {FLOAT_SAMPLES[bi]} a: {ftmp[bi]}).");
			}

			// Short->Short
			{
				var sraw = new RawCodec(AudioEncoding.Pcm, AudioChannels.Mono);
				short[] stmp = new short[SHORT_SAMPLES.Length];
				sraw.Decode(SHORT_SAMPLES.AsSpan().AsBytesUnsafe(), stmp);
				var badIdx = SampleCheck.FindDivergentIndex(SHORT_SAMPLES, stmp, false);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"short->short decode failed at {bi} (e: {SHORT_SAMPLES[bi]} a: {stmp[bi]}).");
			}
		}

		[Fact]
		public void SameFormatEncode_Test()
		{
			// Float->Float
			{
				var fraw = new RawCodec(AudioEncoding.IeeeFloat, AudioChannels.Mono);
				float[] ftmp = new float[FLOAT_SAMPLES.Length];
				fraw.Encode(FLOAT_SAMPLES, ftmp.AsSpan().AsBytesUnsafe());
				var badIdx = SampleCheck.FindDivergentIndex(FLOAT_SAMPLES, ftmp, false);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"float->float encode failed at {bi} (e: {FLOAT_SAMPLES[bi]} a: {ftmp[bi]}).");
			}

			// Short->Short
			{
				var sraw = new RawCodec(AudioEncoding.Pcm, AudioChannels.Mono);
				short[] stmp = new short[SHORT_SAMPLES.Length];
				sraw.Encode(SHORT_SAMPLES, stmp.AsSpan().AsBytesUnsafe());
				var badIdx = SampleCheck.FindDivergentIndex(SHORT_SAMPLES, stmp, false);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"short->short encode failed at {bi} (e: {SHORT_SAMPLES[bi]} a: {stmp[bi]}).");
			}
		}

		[Fact]
		public void DiffFormatDecode_Test()
		{
			// Float->Short
			{
				var fraw = new RawCodec(AudioEncoding.IeeeFloat, AudioChannels.Mono);
				short[] stmp = new short[FLOAT_SAMPLES.Length];
				fraw.Decode(FLOAT_SAMPLES.AsSpan().AsBytesUnsafe(), stmp);
				var badIdx = SampleCheck.FindDivergentIndex(SHORT_SAMPLES, stmp, true);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"float->short decode failed at {bi} (s: {FLOAT_SAMPLES[bi]} e: {SHORT_SAMPLES[bi]} a: {stmp[bi]}).");
			}

			// Short->Float
			{
				var sraw = new RawCodec(AudioEncoding.Pcm, AudioChannels.Mono);
				float[] ftmp = new float[SHORT_SAMPLES.Length];
				sraw.Decode(SHORT_SAMPLES.AsSpan().AsBytesUnsafe(), ftmp);
				var badIdx = SampleCheck.FindDivergentIndex(FLOAT_SAMPLES, ftmp, true);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"short->float decode failed at {bi} (s: {SHORT_SAMPLES[bi]} e: {FLOAT_SAMPLES[bi]} a: {ftmp[bi]}).");
			}
		}

		[Fact]
		public void DiffFormatEncode_Test()
		{
			// Float->Short
			{
				var sraw = new RawCodec(AudioEncoding.Pcm, AudioChannels.Mono);
				short[] stmp = new short[FLOAT_SAMPLES.Length];
				sraw.Encode(FLOAT_SAMPLES, stmp.AsSpan().AsBytesUnsafe());
				var badIdx = SampleCheck.FindDivergentIndex(SHORT_SAMPLES, stmp, true);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"float->short encode failed at {bi} (s: {FLOAT_SAMPLES[bi]} e: {SHORT_SAMPLES[bi]} a: {stmp[bi]}).");
			}

			// Short->Float
			{
				var fraw = new RawCodec(AudioEncoding.IeeeFloat, AudioChannels.Mono);
				float[] ftmp = new float[SHORT_SAMPLES.Length];
				fraw.Encode(SHORT_SAMPLES, ftmp.AsSpan().AsBytesUnsafe());
				var badIdx = SampleCheck.FindDivergentIndex(FLOAT_SAMPLES, ftmp, true);
				Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
					$"short->float encode failed at {bi} (s: {SHORT_SAMPLES[bi]} e: {FLOAT_SAMPLES[bi]} a: {ftmp[bi]}).");
			}
		}
	}
}
