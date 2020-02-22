/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using Xunit;
using SMAL.Rlad;
using SMAL;
using System.Linq;
using System.IO;

namespace Tests.Codec
{
	// Performs testing for RAW codec functions
	public class RladTests
	{
		private const int SAMPLE_COUNT = 512;
		private static readonly short[] SHORT_SAMPLES = new short[SAMPLE_COUNT];
		private static readonly float[] FLOAT_SAMPLES = new float[SAMPLE_COUNT];

		static RladTests()
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
		public void Encode_Test()
		{
			var rlad = new RladCodec(true, AudioChannels.Mono);

			// Require 512 samples to encode
			var ex = Assert.Throws<InvalidOperationException>(() => rlad.Encode(FLOAT_SAMPLES.AsSpan().Slice(0, 1), Span<byte>.Empty));
			Assert.True(ex.Message.StartsWith("RLAD encoding must"), "Encoding failed with unexpected exception.");
		}

		[Fact]
		public void Decode_Test()
		{
			var rlad = new RladCodec(true, AudioChannels.Mono);

			// Require a header to be set before decode
			var ex = Assert.Throws<InvalidOperationException>(() => rlad.Decode(new byte[1], FLOAT_SAMPLES.AsSpan()));
			Assert.True(ex.Message.StartsWith("No block header"), "Decoding failed with unexpected exception.");

			// Check the decode data size
			rlad.BlockHeader = new BlockHeader { DataSize = 1000 };
			var ex2 = Assert.Throws<IncompleteDataException>(() => rlad.Decode(new byte[1], FLOAT_SAMPLES.AsSpan()));
			Assert.True(ex2.Operation == "RLAD data decode", "Decoding failed with bad data size exception.");
		}

		[Fact]
		public void IsLossless_Test()
		{
			var rlad = new RladCodec(true, AudioChannels.Mono);
			var data = new byte[SAMPLE_COUNT * 3];
			var stmp = new short[SHORT_SAMPLES.Length];

			rlad.Encode(SHORT_SAMPLES, data);
			rlad.Decode(data, stmp);

			var badIdx = SampleCheck.FindDivergentIndex(SHORT_SAMPLES, stmp, false);
			Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
				$"Lossless encoding failed at {bi} (e: {SHORT_SAMPLES[bi]} a: {stmp[bi]}).");
		}

		[Fact]
		public void LossyRestore_Test()
		{
			var rlad = new RladCodec(false, AudioChannels.Mono);
			var data = new byte[SAMPLE_COUNT * 3];
			var stmp = new short[SHORT_SAMPLES.Length];
			var lossy = SHORT_SAMPLES.Select(s => (short)((s >> 4) << 4)).ToArray();

			rlad.Encode(SHORT_SAMPLES, data);
			rlad.Decode(data, stmp);

			var badIdx = SampleCheck.FindDivergentIndex(lossy, stmp, false);
			Assert.True(badIdx.GetValueOrDefault(0) is var bi && !badIdx.HasValue,
				$"Lossless encoding failed at {bi} (s: {SHORT_SAMPLES[bi]} e: {lossy[bi]} a: {stmp[bi]}).");
		}

		[Fact]
		public void Layout_Test()
		{
			var rlad = new RladCodec(true, AudioChannels.Mono);
			var data = new byte[2 << 15]; // Big enough
			var samps = new short[512];


			// Tiny,Small,Medium,Full in quarters
			{
				for (int i = 0; i < 512; i += 1)
				{
					if ((i & 1) == 0)
						samps[i] = 0;
					else
						samps[i] = (short)(5 * Math.Pow(10, i / 128)); // 5, 50, 500, 5000
				}

				rlad.Encode(samps, data);
				var runs = rlad.BlockHeader.Value.GetChannelHeaders(0);
				Assert.True(runs.Length == 4, $"Bad number of runs in quarters (e: 4 a: {runs.Length})");
				Assert.True(runs[0].Type == 0, $"Bad type for run[0] (e: 0 a: {runs[0].Type})");
				Assert.True(runs[0].TotalSamples == 128, $"Bad sample count for run[0] (e: 128 a:{runs[0].TotalSamples})");
				Assert.True(runs[1].Type == 1, $"Bad type for run[1] (e: 0 a: {runs[1].Type})");
				Assert.True(runs[1].TotalSamples == 128, $"Bad sample count for run[1] (e: 128 a:{runs[1].TotalSamples})");
				Assert.True(runs[2].Type == 2, $"Bad type for run[2] (e: 0 a: {runs[2].Type})");
				Assert.True(runs[2].TotalSamples == 128, $"Bad sample count for run[2] (e: 128 a:{runs[2].TotalSamples})");
				Assert.True(runs[3].Type == 3, $"Bad type for run[3] (e: 0 a: {runs[3].Type})");
				Assert.True(runs[3].TotalSamples == 128, $"Bad sample count for run[3] (e: 128 a:{runs[3].TotalSamples})");
				Assert.True(rlad.BlockHeader.Value.DataSize == (64 /*T*/ + 128 /*S*/ + 192 /*M*/ + 256 /*F*/),
					$"Bad data size for quarters (e: {64+128+192+256} a: {rlad.BlockHeader.Value.DataSize})");
			}

			// Every run is a different size T->S->M->F
			{
				for (int i = 0; i < 512; i += 1)
				{
					if ((i % 8) == 0)
						samps[i] = (short)(5 * Math.Pow(10, (i % 32) / 8)); // 5, 50, 500, 5000
					else
						samps[i] = 0;
				}

				rlad.Encode(samps, data);
				var runs = rlad.BlockHeader.Value.GetChannelHeaders(0);
				Assert.True(runs.Length == 64, $"Bad number of runs in all-diff (e: 64 a: {runs.Length})");
				for (int i = 0; i < 64; ++i)
				{
					Assert.True(runs[i].Type == (i % 4) && runs[i].Count == 1,
						$"Bad run in all-diff at index {i} ({runs[i]})");
				}
				Assert.True(rlad.BlockHeader.Value.DataSize == (64 /*T*/ + 128 /*S*/ + 192 /*M*/ + 256 /*F*/),
					$"Bad data size for all-diff (e: {64+128+192+256} a: {rlad.BlockHeader.Value.DataSize})");

				// Try again with lossy
				var lossy = new RladCodec(false, AudioChannels.Mono);
				lossy.Encode(samps, data);
				runs = lossy.BlockHeader.Value.GetChannelHeaders(0);
				Assert.True(runs.Length == 64, $"Bad number of runs in lossy all-diff (e: 64 a: {runs.Length})");
				for (int i = 0; i < 64; ++i)
				{
					Assert.True(runs[i].Type == (i % 4) && runs[i].Count == 1,
						$"Bad run in lossy all-diff at index {i} ({runs[i]})");
				}
				Assert.True(lossy.BlockHeader.Value.DataSize == (32 /*T*/ + 64 /*S*/ + 128 /*M*/ + 192 /*F*/),
					$"Bad data size for lossy all-diff (e: {32+64+128+192} a: {lossy.BlockHeader.Value.DataSize})");
			}
		}

		[Fact]
		public void MultiChannel_Test()
		{
			var data = new byte[2 << 15]; // Big enough

			// Stereo recoverability
			{
				var stereo = new short[1024];
				var ret = new short[1024];
				var rand = new Random();
				for (int i = 0; i < 1024; ++i)
					stereo[i] = (short)rand.Next(Int16.MinValue, Int16.MaxValue);

				// Lossless
				var lossl = new RladCodec(true, AudioChannels.Stereo);
				Assert.True(lossl.Encode(stereo, data) == 512, "Stereo lossless recoverability did not encode 512 frames.");
				Assert.True(lossl.Decode(data, ret) == 512, "Stereo lossless recoverability did not decode 512 frames.");
				var bidx = SampleCheck.FindDivergentIndex(stereo, ret);
				Assert.True(bidx.GetValueOrDefault(0) is var bi && !bidx.HasValue,
					$"Stereo lossless recoverability failed at f:{bi/2} s{bi%2} (e: {stereo[bi]} a: {ret[bi]})");

				// Lossy
				var lossy = new RladCodec(false, AudioChannels.Stereo);
				var actual = stereo.Select(s => (short)((s >> 4) << 4)).ToArray();
				Assert.True(lossy.Encode(stereo, data) == 512, "Stereo lossy recoverability did not encode 512 frames.");
				Assert.True(lossy.Decode(data, ret) == 512, "Stereo lossy recoverability did not decode 512 frames.");
				bidx = SampleCheck.FindDivergentIndex(actual, ret);
				Assert.True(bidx.GetValueOrDefault(0) is var bi2 && !bidx.HasValue,
					$"Stereo lossy recoverability failed at f:{bi2/2} s{bi2%2} (s: {stereo[bi2]} e: {actual[bi2]} a: {ret[bi2]})");
			}

			// Stereo data - duplicate left-right
			{
				var stereo = new short[1024];
				var rlad = new RladCodec(true, AudioChannels.Stereo);
				for (int i = 0; i < 512; ++i)
				{
					if ((i % 8) == 0)
						stereo[i*2] = stereo[i*2+1] = (short)(5 * Math.Pow(10, (i % 32) / 8)); // 5, 50, 500, 5000
					else
						stereo[i*2] = stereo[i*2+1] = 0;
				}

				rlad.Encode(stereo, data);
				var runs0 = rlad.BlockHeader.Value.GetChannelHeaders(0);
				var runs1 = rlad.BlockHeader.Value.GetChannelHeaders(1);
				Assert.True(runs0.Length == 64, $"Bad run count for stereo 0 (e: 64 a: {runs0.Length})");
				Assert.True(runs1.Length == 64, $"Bad run count for stereo 1 (e: 64 a: {runs1.Length})");
				for (int i = 0; i < 64; ++i)
				{
					Assert.True(runs0[i].Type == runs1[i].Type && runs0[i].Count == runs1[i].Count,
						$"Bad run match for stereo at index {i} (l: {runs0[i]} r: {runs1[i]})");
				}
			}

			// Stereo data - all-tiny on left, all-full on right
			{
				var stereo = new short[1024];
				for (int i = 0; i < 512; ++i)
				{
					if ((i & 1) == 0)
						stereo[i*2] = stereo[i*2 + 1] = 0;
					else
					{
						stereo[i*2]   = 5;
						stereo[i*2+1] = 5000;
					}
				}

				var rlad = new RladCodec(true, AudioChannels.Stereo);
				rlad.Encode(stereo, data);
				var runs0 = rlad.BlockHeader.Value.GetChannelHeaders(0);
				var runs1 = rlad.BlockHeader.Value.GetChannelHeaders(1);
				Assert.True(runs0.Length == 1, $"Bad length for stereo all-tiny ({runs0.Length})");
				Assert.True(runs0[0].Type == 0 && runs0[0].TotalSamples == 512,
					$"Bad run for stereo all-tiny ({runs0[0]})");
				Assert.True(runs1.Length == 1, $"Bad length for stereo all-full ({runs1.Length})");
				Assert.True(runs1[0].Type == 3 && runs1[0].TotalSamples == 512,
					$"Bad run for stereo all-full ({runs1[0]})");
				Assert.True(rlad.BlockHeader.Value.DataSize == (256 /*T*/ + 1024 /*F*/),
					$"Bad data size for stereo all-tiny/all-full (e:{256+1024} a:{rlad.BlockHeader.Value.DataSize})");
			}
		}

		[Fact]
		public void HeaderIO_Test()
		{
			MemoryStream stream = new MemoryStream(new byte[1024], true);
			var rand = new Random();

			// Create a test header
			BlockHeader header = new BlockHeader { DataSize = 12345 };
			header.SetChannelCount(0, 2);
			header.SetChannelCount(1, 5);
			header.SetChannelCount(2, 13);
			header.SetChannelCount(3, 64);
			var r0 = header.GetChannelHeaders(0);
			var r1 = header.GetChannelHeaders(1);
			var r2 = header.GetChannelHeaders(2);
			var r3 = header.GetChannelHeaders(3);
			for (int i = 0; i < r0.Length; ++i)
				r0[i] = new RunHeader(rand.Next(0, 3), rand.Next(0, 63));
			for (int i = 0; i < r1.Length; ++i)
				r1[i] = new RunHeader(rand.Next(0, 3), rand.Next(0, 63));
			for (int i = 0; i < r2.Length; ++i)
				r2[i] = new RunHeader(rand.Next(0, 3), rand.Next(0, 63));
			for (int i = 0; i < r3.Length; ++i)
				r3[i] = new RunHeader(rand.Next(0, 3), rand.Next(0, 63));

			// Write, then read, the header
			BlockHeader.Write(stream, AudioChannels.Quadraphonic, ref header);
			BlockHeader header2 = default;
			stream.Position = 0;
			BlockHeader.Read(stream, AudioChannels.Quadraphonic, ref header2);

			// Check
			Assert.True(header.DataSize == header2.DataSize, $"Mismatched DataSize");
			for (byte i = 0; i < 4; ++i)
			{
				var run = header.GetChannelHeaders(i);
				var run2 = header2.GetChannelHeaders(i);

				Assert.True(run.Length == run2.Length, $"Mismatched channel count for channel {i}");
				for (int r = 0; r < run.Length; ++r)
					Assert.True(run[i] == run2[i], $"Mismatched run ch:{i} r{r} ({run[i]} != {run2[i]})");
			}
		}
	}
}
