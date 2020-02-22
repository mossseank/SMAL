/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using System.IO;

namespace SMAL.Rlad
{
	/// <summary>
	/// Contains the set of information found within an RLAD stream for header data.
	/// </summary>
	public sealed class RladHeader
	{
		private const uint TAG_RLAD = 0x44414C52; // "RLAD" (little endian)
		private const byte VALUE_FALSE = 0x00;
		private const byte VALUE_TRUE = 0xFF;

		#region Fields
		/// <summary>
		/// The encoding of the data, will be either <see cref="AudioEncoding.RLAD"/> or 
		/// <see cref="AudioEncoding.RLADLossy"/>.
		/// </summary>
		public AudioEncoding Format;
		/// <summary>
		/// The channels present in the data.
		/// </summary>
		public AudioChannels Channels;
		/// <summary>
		/// The number of frames in the last block.
		/// </summary>
		public ushort LastBlockFrames;
		/// <summary>
		/// The sample rate of the data.
		/// </summary>
		public uint SampleRate;
		/// <summary>
		/// The total number of available audio blocks (512 sample groups).
		/// </summary>
		public uint BlockCount;
		/// <summary>
		/// The total number of available audio frames.
		/// </summary>
		public uint FrameCount => ((BlockCount - 1) * 512u) + LastBlockFrames;
		#endregion // Fields

		/// <summary>
		/// Loads a new header from the stream.
		/// </summary>
		/// <param name="stream">The stream to load the header from.</param>
		/// <returns>The new header parsed from the stream.</returns>
		/// <exception cref="BadFormatException">The stream does not contain a valid RLAD header.</exception>
		/// <exception cref="EndOfStreamException">The stream ended before the header was parsed.</exception>
		public static RladHeader Read(Stream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			if (!stream.CanRead)
				throw new ArgumentException("Cannot load from unreadable stream", nameof(stream));

			// Read header
			Span<uint> header = stackalloc uint[4];
			if (stream.Read(header.AsBytesUnsafe()) != (sizeof(uint) * 4))
				throw new EndOfStreamException();

			// Parse header
			if (header[0] != TAG_RLAD)
				throw new BadFormatException("RLAD", "RLAD tag missing");
			var format = (byte)(header[1] & 0xFF) switch { 
				VALUE_FALSE => AudioEncoding.RLADLossy,
				VALUE_TRUE => AudioEncoding.RLAD,
				_ => throw new BadFormatException("RLAD", "lossy flag not valid")
			};
			var channels = (AudioChannels)((header[1] >> 8) & 0xFF);
			if (!Enum.IsDefined(typeof(AudioChannels), channels))
				throw new BadFormatException("RLAD", $"invalid channel count {(int)channels}");

			return new RladHeader {
				Format = format,
				Channels = channels,
				LastBlockFrames = (ushort)((header[1] >> 16) & 0xFFFF),
				SampleRate = header[2],
				BlockCount = header[3]
			};
		}

		/// <summary>
		/// Writes the RLAD header to the stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="header">The header to write.</param>
		public static void Write(Stream stream, RladHeader header)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			if (header is null)
				throw new ArgumentNullException(nameof(header));
			if (!stream.CanWrite)
				throw new ArgumentException("Cannot write to readonly stream", nameof(stream));

			// Prepare the header
			uint lossless = (header.Format == AudioEncoding.RLADLossy) ? VALUE_FALSE : VALUE_TRUE;
			uint channels = ((uint)header.Channels << 8);
			uint last = ((uint)header.LastBlockFrames << 16);
			Span<uint> data = stackalloc uint[4] { 
				TAG_RLAD,
				(last | channels | lossless),
				header.SampleRate,
				header.BlockCount
			};

			// Write
			stream.Write(data.AsBytesUnsafe());
		}
	}
}
