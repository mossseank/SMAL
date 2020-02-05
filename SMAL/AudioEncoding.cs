/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL
{
	/// <summary>
	/// Enumerates all supported audio coding formats.
	/// </summary>
	public enum AudioEncoding : byte
	{
		/// <summary>
		/// Linear pulse code modulation.
		/// </summary>
		Pcm = 0x00,
		/// <summary>
		/// Linear pulse code modulation using 32-bit IEEE floating point values.
		/// </summary>
		IeeeFloat = 0x01,
		/// <summary>
		/// Adaptive differential pulse code modulation.
		/// </summary>
		Adpcm = 0x02,

		// Reserve up to 0x0F for future Wave file formats

		/// <summary>
		/// Lossless Run-Length Accumulating Deltas
		/// </summary>
		RLAD = 0x10,
		/// <summary>
		/// Lossy Run-Length Accumulating Deltas
		/// </summary>
		RLADLossy = 0x11,

		/// <summary>
		/// Xiph.Org (Ogg) Vorbis
		/// </summary>
		Vorbis = 0x20,

		/// <summary>
		/// Xiph.Org Free Lossless Audio Codec
		/// </summary>
		FLAC = 0x30,

		/// <summary>
		/// Xiph.Org Opus
		/// </summary>
		Opus = 0x40,

		/// <summary>
		/// Unknown encoding
		/// </summary>
		Unknown = 0xFF
	}
}
