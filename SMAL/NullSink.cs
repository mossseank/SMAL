/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL
{
	/// <summary>
	/// Provides no-op sample sink functionality, consuming sample buffers instantly with no additional processing.
	/// </summary>
	public sealed class NullSink : ISampleSink
	{
		#region Fields
		/// <inheritdoc/>
		public AudioChannels Channels { get; }
		#endregion // Fields

		/// <summary>
		/// Creates a new null sink for the given channel set.
		/// </summary>
		/// <param name="channels">The channel set to consume.</param>
		public NullSink(AudioChannels channels) =>
			Channels = channels;

		/// <summary>
		/// No-op consumption of audio samples.
		/// </summary>
		/// <param name="buffer">The audio sample buffer.</param>
		/// <returns>The buffer length rounded to the nearest multiple of the channel count.</returns>
		public uint PutSamples(Span<short> buffer) => (uint)(buffer.Length - (buffer.Length % (int)Channels));

		/// <summary>
		/// No-op consumption of audio samples.
		/// </summary>
		/// <param name="buffer">The audio sample buffer.</param>
		/// <returns>The buffer length rounded to the nearest multiple of the channel count.</returns>
		public uint PutSamples(Span<float> buffer) => (uint)(buffer.Length - (buffer.Length % (int)Channels));
	}
}
