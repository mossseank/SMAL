/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL.Gen
{
	/// <summary>
	/// Describes the features of a single-freqency audio tone.
	/// </summary>
	public struct Tone
	{
		#region Fields
		/// <summary>
		/// The shape of the tone waveform.
		/// </summary>
		public Waveform Waveform;
		/// <summary>
		/// The frequency of the tone in Hz.
		/// </summary>
		public float Frequency;
		/// <summary>
		/// The adjusted amplitude of the tone, in the range 0 to 1.
		/// </summary>
		public float Amplitude
		{
			readonly get => _amplitude.GetValueOrDefault(1);
			set => _amplitude = Math.Clamp(value, 0, 1);
		}
		private float? _amplitude;
		#endregion // Fields

		/// <summary>
		/// Describes a tone with the given frequency and waveform.
		/// </summary>
		/// <param name="freq">The tone frequency.</param>
		/// <param name="form">The tone waveform.</param>
		public Tone(float freq, Waveform form = Waveform.Sine)
		{
			Waveform = form;
			Frequency = freq;
			_amplitude = null;
		}

		/// <summary>
		/// Describes a tone with the given frequency, amplitude, and waveform.
		/// </summary>
		/// <param name="freq">The tone frequency.</param>
		/// <param name="amp">The tone amplitude (will be clamped to [0, 1]).</param>
		/// <param name="form">The tone waveform.</param>
		public Tone(float freq, float amp, Waveform form = Waveform.Sine)
		{
			Waveform = form;
			Frequency = freq;
			_amplitude = Math.Clamp(amp, 0, 1);
		}
	}
}
