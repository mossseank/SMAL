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
		/// The channel set for audio data produced by this source.
		/// </summary>
		AudioChannels Channels { get; }

		/// <summary>
		/// The sampling rate for the produced audio data. Should be zero (0) for sources that are not tied to a
		/// set sample rate.
		/// </summary>
		uint SampleRate { get; }

		/// <summary>
		/// Attempts to fill the buffer with the next set of available audio samples, in signed 16-bit integer LPCM
		/// format.
		/// </summary>
		/// <param name="buffer">
		/// The buffer to fill with samples. It is undefined behavior if the sample count is not a multiple of
		/// <see cref="Channels"/>.
		/// </param>
		/// <returns>The actual number of frames placed into the buffer.</returns>
		uint GetSamples(Span<short> buffer);

		/// <summary>
		/// Attempts to fill the buffer with the next set of available audio samples, in normalized 32-bit floating
		/// point LPCM format.
		/// </summary>
		/// <param name="buffer">
		/// The buffer to fill with samples. It is undefined behavior if the sample count is not a multiple of
		/// <see cref="Channels"/>.
		/// </param>
		/// <returns>The actual number of frames placed into the buffer.</returns>
		uint GetSamples(Span<float> buffer);
	}
}
