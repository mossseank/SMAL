/*
 * MIT License (Ms-PL) - Copyright (c) 2020 SMAL Authors
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
		/// The total number of frames (time-coincident samples) available in this source. A special value of
		/// <see cref="UInt32.MaxValue"/> represents a source that can provide unlimited frames (such as an audio
		/// generator).
		/// </summary>
		uint TotalFrames { get; }

		/// <summary>
		/// Attempts to fill the buffer with the next set of available audio samples, in signed 16-bit integer LPCM
		/// format.
		/// </summary>
		/// <param name="buffer">The buffer to fill with samples.</param>
		/// <returns>The actual number of samples placed into the buffer.</returns>
		uint GetSamples(Span<short> buffer);

		/// <summary>
		/// Attempts to fill the buffer with the next set of available audio samples, in normalized 32-bit floating
		/// point LPCM format.
		/// </summary>
		/// <param name="buffer">The buffer to fill with samples.</param>
		/// <returns>The actual number of samples placed into the buffer.</returns>
		uint GetSamples(Span<float> buffer);
	}
}
