/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace Tests
{
	// Utilities for checking audio sample values
	internal static class SampleCheck
	{
		private const float EPSILON = 2 * Single.Epsilon;
		// Difference of 2 steps allowed for conversion between formats, and then back
		private const float FLOAT_ROUNDING_ERROR = 2f / UInt16.MaxValue;
		private const int SHORT_ROUNDING_ERROR = 2;

		// Finds the first index with divergent values, optionally allowing off-by-one errors
		public static uint? FindDivergentIndex(ReadOnlySpan<float> l, ReadOnlySpan<float> r, bool allowRoundingError = true)
		{
			var len = Math.Min(l.Length, r.Length);
			for (int i = 0; i < len; ++i)
			{
				float diff = Math.Abs(l[i] - r[i]);
				if (diff <= EPSILON)
					continue;
				if (allowRoundingError && (diff <= FLOAT_ROUNDING_ERROR))
					continue;
				return (uint)i;
			}
			return null;
		}

		// Finds the first index with divergent values, optionally allowing off-by-one errors
		public static uint? FindDivergentIndex(ReadOnlySpan<short> l, ReadOnlySpan<short> r, bool allowRoundingError = true)
		{
			var len = Math.Min(l.Length, r.Length);
			for (int i = 0; i < len; ++i)
			{
				int diff = Math.Abs(l[i] - r[i]);
				if (diff == 0)
					continue;
				if (allowRoundingError && (diff <= SHORT_ROUNDING_ERROR))
					continue;
				return (uint)i;
			}
			return null;
		}
	}
}
