/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using System.IO;

namespace SMAL.Wave
{
	/// <summary>
	/// Contains the set of information found within a RIFF stream for WAVE data.
	/// </summary>
	public sealed class WaveHeader
	{
		private const uint TAG_RIFF = 0x46464952; // "RIFF" (little endian)
		private const uint TAG_WAVE = 0x45564157; // "WAVE" (little endian)
		private const uint TAG_FMT  = 0x20746D66; // "fmt " (little endian)
		private const uint TAG_DATA = 0x61746164; // "data" (little endian)
		private const ushort FMT_PCM       = 0x0001; // PCM format
		private const ushort FMT_IEEEFLOAT = 0x0003; // IeeeFloat format

		#region Fields
		/// <summary>
		/// The total size of the file, minus 8 bytes for the RIFF header.
		/// </summary>
		public uint ChunkSize;
		/// <summary>
		/// The audio data format, or <see cref="AudioEncoding.Unknown"/> for unsupported formats.
		/// </summary>
		public AudioEncoding Format;
		/// <summary>
		/// The audio channel set for the data.
		/// </summary>
		public AudioChannels Channels;
		/// <summary>
		/// The samping rate for the data.
		/// </summary>
		public uint SampleRate;
		/// <summary>
		/// The offset (in bytes) into the stream where the data starts.
		/// </summary>
		public uint DataStart;
		/// <summary>
		/// The number of audio data frames.
		/// </summary>
		public uint FrameCount;
		#endregion // Fields

		/// <summary>
		/// Attempts to load a WAVE header from the stream.
		/// </summary>
		/// <param name="stream">The stream to load from.</param>
		/// <returns>The WAVE header read from the stream.</returns>
		/// <exception cref="BadFormatException">The stream does not contain a valid WAVE header.</exception>
		/// <exception cref="EndOfStreamException">The stream ended before the header was parsed.</exception>
		public static WaveHeader Read(Stream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			if (!stream.CanRead)
				throw new ArgumentException("Cannot load from unreadable stream", nameof(stream));

			// Check RIFF header
			Span<uint> riff = stackalloc uint[3];
			if (stream.Read(riff.AsBytesUnsafe()) != (sizeof(uint) * 3))
				throw new EndOfStreamException();
			if (riff[0] != TAG_RIFF)
				throw new BadFormatException("RIFF", "RIFF tag not found");
			if (riff[2] != TAG_WAVE)
				throw new BadFormatException("RIFF", "RIFF file is not a WAVE file");

			// Pull out wave "fmt " header
			Span<uint> fmt = stackalloc uint[6];
			if (stream.Read(fmt.AsBytesUnsafe()) != (sizeof(uint) * 6))
				throw new EndOfStreamException();
			if (fmt[0] != TAG_FMT)
				throw new BadFormatException("WAVE", "'fmt' chunk header not found");
			var fmtCode = (ushort)(fmt[2] & 0xFFFF);
			var channels = (ushort)((fmt[2] >> 16) & 0xFFFF);
			var byps = (ushort)((fmt[5] >> 16) & 0xFFFF) / 8;

			// Discard any remaining "fmt " header fields
			{
				uint rem = fmt[1] - 16;
				while (rem-- > 0)
					stream.ReadByte();
			}

			// Validate the format fields
			var format = (fmtCode, byps) switch {
				(FMT_PCM, 2) => AudioEncoding.Pcm,
				(FMT_IEEEFLOAT, 4) => AudioEncoding.IeeeFloat,
				_ => AudioEncoding.Unknown
			};
			if (!Enum.IsDefined(typeof(AudioChannels), (AudioChannels)channels))
				throw new BadFormatException("WAVE", $"Invalid channel count ({channels})");

			// Scan until the data chunk
			Span<uint> subchunk = stackalloc uint[2];
			uint dataSize = 0;
			uint dataStart = 0;
			do
			{
				if (stream.Read(subchunk.AsBytesUnsafe()) != (sizeof(uint) * 2))
					throw new EndOfStreamException();

				if (subchunk[0] == TAG_DATA)
				{
					dataSize = subchunk[1];
					dataStart = (uint)stream.Position;
					break;
				}
				else
					stream.Seek(subchunk[1], SeekOrigin.Current);
			}
			while (true);

			// Return
			return new WaveHeader { 
				ChunkSize = riff[1],
				Format = format,
				Channels = (AudioChannels)channels,
				SampleRate = fmt[3],
				DataStart = dataStart,
				FrameCount = (uint)(dataSize / byps / channels)
			};
		}

		/// <summary>
		/// Writes the WAVE header to the stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="header">The header to write.</param>
		public static void Write(Stream stream, WaveHeader header)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			if (header is null)
				throw new ArgumentNullException(nameof(header));
			if (!stream.CanWrite)
				throw new ArgumentException($"Cannot write to read-only stream", nameof(stream));

			// Write the RIFF header
			Span<uint> riff = stackalloc uint[3] { TAG_RIFF, header.ChunkSize, TAG_WAVE };
			stream.Write(riff.AsBytesUnsafe());

			// Write the fmt header
			uint byps = (header.Format == AudioEncoding.Pcm) ? 2u : 4u;
			uint format = (header.Format == AudioEncoding.Pcm) ? FMT_PCM : FMT_IEEEFLOAT;
			Span<uint> fmt = stackalloc uint[6] { 
				TAG_FMT,
				16,
				((uint)header.Channels << 16) | format,
				header.SampleRate,
				header.SampleRate * (uint)header.Channels * byps,
				((uint)header.Channels * byps) | ((byps * 8) << 16)
			};
			stream.Write(fmt.AsBytesUnsafe());

			// Write the data chunk header
			Span<uint> data = stackalloc uint[2] { TAG_DATA, header.FrameCount * (uint)header.Channels * byps };
			stream.Read(data.AsBytesUnsafe());
		}
	}
}
