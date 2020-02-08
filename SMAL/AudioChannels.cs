/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL
{
	/// <summary>
	/// Enumerates all supported audio channel configurations. These values can be directly cast to their integer
	/// channel count.
	/// </summary>
	public enum AudioChannels : byte
	{
		/// <summary>
		/// Single-channel audio.
		/// </summary>
		Mono = 1,
		/// <summary>
		/// Dual-channel L/R audio.
		/// </summary>
		Stereo = 2,
		/// <summary>
		/// Four-channel FL/FR/BL/BR audio.
		/// </summary>
		Quadraphonic = 4,
		/// <summary>
		/// 5.1 surround sound, 6-channel FL/FR/FC/Low/BL/BR audio.
		/// </summary>
		FiveOne = 6,
		/// <summary>
		/// 7.1 surround sound, 8-channel FL/FR/FC/Low/SL/SR/BL/BR audio.
		/// </summary>
		SevenOne = 8
	}
}
