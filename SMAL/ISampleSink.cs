/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL
{
	/// <summary>
	/// Interface for types that can provide a write sink for audio samples.
	/// </summary>
	public interface ISampleSink
	{
		/// <summary>
		/// The number of channels of audio data consumed by this source.
		/// </summary>
		uint ChannelCount { get; }

		/// <summary>
		/// Attempts to consume 16-bit signed integer LPCM samples from the buffer.
		/// </summary>
		/// <param name="buffer">
		/// The buffer to pull samples from. It is undefined behavior if the sample count is not a multiple of
		/// <see cref="ChannelCount"/>.
		/// </param>
		/// <returns>The actual number of frames read from the buffer.</returns>
		uint PutSamples(Span<short> buffer);

		/// <summary>
		/// Attempts to consume 32-bit normalized floating point LPCM samples from the buffer.
		/// </summary>
		/// <param name="buffer">
		/// The buffer to pull samples from. It is undefined behavior if the sample count is not a multiple of
		/// <see cref="ChannelCount"/>.
		/// </param>
		/// <returns>The actual number of frames read from the buffer.</returns>
		uint PutSamples(Span<float> buffer);
	}
}
