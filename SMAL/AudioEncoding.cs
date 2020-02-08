/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using System.Runtime.CompilerServices;

namespace SMAL
{
	/// <summary>
	/// Enumerates all supported audio coding formats.
	/// </summary>
	public enum AudioEncoding : byte
	{
		/// <summary>
		/// Linear pulse code modulation using 16-bit signed integer values.
		/// </summary>
		Pcm = 0x00, // (RIFF format 0x0001)
		/// <summary>
		/// Linear pulse code modulation using 32-bit IEEE floating point values.
		/// </summary>
		IeeeFloat = 0x01, // (RIFF format 0x0003)

		// Reserve up to 0x0F for future RAW formats

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

	/// <summary>
	/// Extension and utility functionality for working with <see cref="AudioEncoding"/> values.
	/// </summary>
	public static class AudioEncodingExtensions
	{
		/// <summary>
		/// Gets if the encoding represents a RAW (Wave) format.
		/// </summary>
		/// <param name="enc">The encoding to check.</param>
		/// <returns>If the encoding is a RAW format.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsRawFormat(this AudioEncoding enc) => ((int)enc & 0xF0) == 0;

		/// <summary>
		/// Gets if the encoding is a lossy compression format.
		/// </summary>
		/// <param name="enc">The encoding to check.</param>
		/// <returns>If the encoding is lossy (original data cannot be rebuilt perfectly).</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsLossy(this AudioEncoding enc) => enc switch {
			AudioEncoding.RLADLossy => true,
			AudioEncoding.Vorbis => true,
			AudioEncoding.Opus => true,
			_ => false
		};

		/// <summary>
		/// Gets if the encoding is a lossless compression format.
		/// </summary>
		/// <param name="enc">The encoding to check.</param>
		/// <returns>If the encoding is lossless (original data can be rebuilt perfectly).</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsLossless(this AudioEncoding enc) => !IsLossy(enc);
	}
}
