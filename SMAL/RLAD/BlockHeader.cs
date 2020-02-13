/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SMAL.RLAD
{
	/// <summary>
	/// Contains header information for an RLAD block within a stream.
	/// </summary>
	public unsafe struct BlockHeader
	{
		private const int MAX_RUNS_PER_CHANNEL = 64; // 512 samples / 8 samples per run
		private const int MAX_CHANNELS = 8;

		#region Fields
		/// <summary>
		/// The total size of the block (including all headers) in bytes.
		/// </summary>
		public ushort BlockSize;
		/// <summary>
		/// The total size of the compressed audio data in the block, in bytes.
		/// </summary>
		public ushort DataSize;
		
		private fixed byte _counts[MAX_CHANNELS];
		private fixed byte _headers[MAX_RUNS_PER_CHANNEL * MAX_CHANNELS];
		#endregion // Fields

		/// <summary>
		/// Gets the run headers for the passed channel index.
		/// </summary>
		/// <param name="channel">The channel to get, must be in the range [0, 7].</param>
		/// <returns>The run headers for the channel data.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<RunHeader> GetChannelHeaders(byte channel) => (channel < MAX_CHANNELS)
			? new Span<RunHeader>(Unsafe.AsPointer(ref _headers[channel * MAX_RUNS_PER_CHANNEL]), _counts[channel])
			: throw new ArgumentOutOfRangeException(nameof(channel));

		/// <summary>
		/// Parses an RLAD block header from the stream.
		/// </summary>
		/// <param name="stream">The stream to parse the header from.</param>
		/// <param name="channels">The number of channels for the block.</param>
		/// <param name="block">The object to place the resulting header info into.</param>
		/// <returns>The number of bytes read from the stream.</returns>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static uint Read(Stream stream, AudioChannels channels, ref BlockHeader block)
		{
			uint read = 0;

			// Read the header
			Span<ushort> header = stackalloc ushort[2];
			if (stream.Read(header.AsBytesUnsafe()) != 4)
				throw new IncompleteHeaderException("RLAD block - block sizes");
			block.BlockSize = header[0];
			block.DataSize = header[1];
			read += 4;

			// Read the run counts
			var counts = new Span<byte>(Unsafe.AsPointer(ref block._counts[0]), (int)channels);
			if (stream.Read(counts) != counts.Length)
				throw new IncompleteHeaderException("RLAD block - run header counts");
			read += (uint)channels;

			// Read each of the run header sets
			var rheads = new Span<byte>(Unsafe.AsPointer(ref block._headers[0]), MAX_RUNS_PER_CHANNEL * MAX_CHANNELS);
			for (int ch = 0; ch < (int)channels; ++ch)
			{
				if (stream.Read(rheads.Slice(ch * MAX_RUNS_PER_CHANNEL, counts[ch])) != counts[ch])
					throw new IncompleteHeaderException("RLAD block - run headers");
				read += counts[ch];
			}

			return read;
		}

		/// <summary>
		/// Writes an RLAD block header to the stream.
		/// </summary>
		/// <param name="stream">The stream to write the header to.</param>
		/// <param name="channels">The number of audio channels.</param>
		/// <param name="block">The header to write.</param>
		/// <returns>The number of bytes written to the stream.</returns>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public static uint Write(Stream stream, AudioChannels channels, ref BlockHeader block)
		{
			uint written = 0;

			// Write the header
			Span<ushort> header = stackalloc ushort[2];
			header[0] = block.BlockSize;
			header[1] = block.DataSize;
			stream.Write(header.AsBytesUnsafe());
			written += 4;

			// Write the run counts
			var counts = new ReadOnlySpan<byte>(Unsafe.AsPointer(ref block._counts[0]), (int)channels);
			stream.Write(counts);
			written += (uint)channels;

			// Write the run header sets
			var rheads = new ReadOnlySpan<byte>(Unsafe.AsPointer(ref block._headers[0]), 
				MAX_RUNS_PER_CHANNEL * MAX_CHANNELS);
			for (int ch = 0; ch < (int)channels; ++ch)
			{
				stream.Write(rheads.Slice(ch * MAX_RUNS_PER_CHANNEL, counts[ch]));
				written += counts[ch];
			}

			return written;
		}
	}
}
