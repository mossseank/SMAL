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
	/// <see cref="AudioReader"/> specialization for reading audio from a stream that contains RLAD (.rlad) encoded
	/// data.
	/// </summary>
	public sealed class RladReader : AudioReader
	{
		private const uint CHANNEL_DATA_SIZE = 1024; // 512 samples of 2 byte data
		private const uint FRAMES_PER_BLOCK = 512;

		#region Fields
		/// <inheritdoc/>
		public override uint FrameCount => _header.FrameCount;
		/// <inheritdoc/>
		public override AudioChannels Channels => _header.Channels;
		/// <inheritdoc/>
		public override uint SampleRate => _header.SampleRate;

		private readonly byte[] _rawDataBuffer; // Buffer for raw data read from the file

		private readonly RladHeader _header; // The header loaded from the file
		private readonly RladCodec _codec; // The codec used to read the samples
		private BlockHeader _block; // The header for the current decoded block
		private uint _blockSize => CHANNEL_DATA_SIZE * ChannelCount; // Size of a decoded block in bytes
		#endregion // Fields

		/// <summary>
		/// Creates a new reader for the file at the given path.
		/// </summary>
		/// <param name="path">The path to the RLAD file to load.</param>
		public RladReader(string path) :
			this(File.OpenRead(path))
		{ }

		/// <summary>
		/// Creates a new reader for the stream.
		/// </summary>
		/// <param name="stream">The stream to load RLAD data from.</param>
		public RladReader(Stream stream) :
			base(stream, 0)
		{
			_header = RladHeader.Read(stream);
			_codec = new RladCodec(_header.Format == AudioEncoding.RLAD, _header.Channels);
			_block = default;
			EnsureOverflowSize(_blockSize);
			_rawDataBuffer = new byte[_blockSize];
		}

		protected unsafe override (uint Frames, uint Overflow, bool OverflowFloat) ReadSamples(Span<byte> dst, uint frames, bool dstFloat)
		{
			frames = Math.Min(frames, Remaining);
			uint fullBlockCount = frames / FRAMES_PER_BLOCK;
			uint extraFrames = frames % FRAMES_PER_BLOCK;
			int dstOff = 0;
			var rawDataSpan = _rawDataBuffer.AsSpan();
			uint total = 0;

			// Perform the direct buffer writes
			uint bidx = 0;
			while ((bidx++) < fullBlockCount)
			{
				// Read in the block header and the data
				BlockHeader.Read(Stream, Channels, ref _block);
				int readSize = Stream.Read(_rawDataBuffer, 0, _block.DataSize);
				if (readSize != _block.DataSize)
					throw new IncompleteDataException("block data read", (uint)(_block.DataSize - readSize));

				if (_block.IsLastBlock)
				{
					_codec.Decode(rawDataSpan, OverflowShort);
					int remSamp = _header.LastBlockFrames * (int)ChannelCount;
					if (dstFloat)
						SampleUtils.Convert(OverflowShort.Slice(0, remSamp), dst.Slice(dstOff, remSamp * 4).UnsafeCast<float>());
					else
						OverflowShort.Slice(0, remSamp).CopyTo(dst.Slice(dstOff, remSamp * 2).UnsafeCast<short>());
					return (total + _header.LastBlockFrames, 0, false);
				}
				else
				{
					if (dstFloat)
						_codec.Decode(rawDataSpan, dst.Slice(dstOff, (int)_blockSize).UnsafeCast<float>());
					else
						_codec.Decode(rawDataSpan, dst.Slice(dstOff, (int)_blockSize).UnsafeCast<short>());
					dstOff += (int)_blockSize;
					total += FRAMES_PER_BLOCK;
				}
			}

			// Decode the last block, with overflow
			uint inOver = 0;
			if (extraFrames != 0)
			{
				// Read in the block header and the data
				BlockHeader.Read(Stream, Channels, ref _block);
				int readSize = Stream.Read(_rawDataBuffer, 0, _block.DataSize);
				if (readSize != _block.DataSize)
					throw new IncompleteDataException("block data read", (uint)(_block.DataSize - readSize));

				// Decode to tmp buffer
				_codec.Decode(_rawDataBuffer, OverflowShort);
				int remSamp = (int)(extraFrames * ChannelCount);
				if (dstFloat)
					SampleUtils.Convert(OverflowShort.Slice(0, remSamp), dst.Slice(dstOff, remSamp * 4).UnsafeCast<float>());
				else
					OverflowShort.Slice(0, remSamp).CopyTo(dst.Slice(dstOff, remSamp * 2).UnsafeCast<short>());
				total += extraFrames;

				// Move the samples within the overflow buffer
				inOver = ((_block.IsLastBlock ? _header.LastBlockFrames : FRAMES_PER_BLOCK) - extraFrames);
				int shiftCount = (int)(inOver * ChannelCount);
				if (shiftCount != 0)
					OverflowShort.Slice(remSamp, shiftCount).CopyTo(OverflowShort);
			}

			return (total, inOver, false);
		}

		protected override void OnDispose(bool disposing) { }
	}
}
