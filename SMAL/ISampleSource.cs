/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL
{
	/// <summary>
	/// Interface for types that can provide a source of audio samples.
	/// </summary>
	public interface ISampleSource
	{
		/// <summary>
		/// The number of channels of audio data produced by this source.
		/// </summary>
		uint ChannelCount { get; }

		/// <summary>
		/// Attempts to fill the buffer with the next set of available audio samples, in signed 16-bit integer LPCM
		/// format.
		/// </summary>
		/// <param name="buffer">
		/// The buffer to fill with samples. Its length will be rounded down to the nearest multiple of 
		/// <see cref="ChannelCount"/>, if needed.
		/// </param>
		/// <returns>The actual number of samples placed into the buffer.</returns>
		uint GetSamples(Span<short> buffer);

		/// <summary>
		/// Attempts to fill the buffer with the next set of available audio samples, in normalized 32-bit floating
		/// point LPCM format.
		/// </summary>
		/// <param name="buffer">
		/// The buffer to fill with samples. Its length will be rounded down to the nearest multiple of 
		/// <see cref="ChannelCount"/>, if needed.
		/// </param>
		/// <returns>The actual number of samples placed into the buffer.</returns>
		uint GetSamples(Span<float> buffer);
	}
}
