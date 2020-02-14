/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL.Gen
{
	/// <summary>
	/// Enumerates the different waveform shapes that can be generated.
	/// </summary>
	public enum Waveform : byte
	{
		/// <summary>
		/// A standard smooth mathematical sine wave.
		/// </summary>
		Sine,
		/// <summary>
		/// A repeating cycle linearly increasing and decreasing segments, creating a "zig-zag" pattern.
		/// </summary>
		Triangular,
		/// <summary>
		/// A cycle that linearly moves from minimum value to maximum value, then jumps back to minimum.
		/// </summary>
		Sawtooth
	}
}
