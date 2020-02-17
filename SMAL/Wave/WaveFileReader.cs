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
	/// <see cref="AudioReader"/> specialization for reading audio data from a WAVE (.wav) file.
	/// </summary>
	public sealed class WaveFileReader : AudioReader
	{
		#region Fields
		/// <inheritdoc/>
		public override uint FrameCount => _header.FrameCount;
		/// <inheritdoc/>
		public override AudioChannels Channels => _header.Channels;
		/// <inheritdoc/>
		public override uint SampleRate => _header.SampleRate;

		private readonly WaveHeader _header; // The header loaded from the file
		private readonly RawCodec _codec; // The codec used to read the samples
		#endregion // Fields

		/// <summary>
		/// Creates a new reader for the file at the given path.
		/// </summary>
		/// <param name="path">The path to the WAVE file to load.</param>
		public WaveFileReader(string path) :
			this(File.OpenRead(path))
		{ }

		/// <summary>
		/// Creates a new reader for the file opened in the stream.
		/// </summary>
		/// <param name="stream">The file stream to load WAVE data from.</param>
		public WaveFileReader(FileStream stream) :
			base(stream, 0)
		{
			_header = WaveHeader.Read(stream);
			_codec = new RawCodec(_header.Format, _header.Channels);
			
			// Get to the start of the audio data
			if (stream.Position != _header.DataStart)
			{
				long diff = _header.DataStart - stream.Position;
				if (stream.CanSeek)
				{
					if (stream.Seek(diff, SeekOrigin.Current) != _header.DataStart)
						throw new IOException("Unable to seek to start of WAVE data");
				}
				else if (diff > 0)
				{
					// Forward-seek using reads
					while (diff-- > 0)
					{
						if (stream.ReadByte() == -1)
							throw new EndOfStreamException();
					}
				}
				else // Data is behind us, and we can't seek backwards
					throw new IOException("Unable to seek to start of WAVE data (malformed file?)");
			}
		}

		protected override (uint Frames, uint Overflow, bool OverflowFloat) ReadSamples(Span<byte> dst, 
			uint frames, bool dstFloat)
		{
			bool srcFloat = (_header.Format == AudioEncoding.IeeeFloat);

			if (dstFloat == srcFloat) // Nice shortcut - bypass codec by reading directly to buffer
			{
				int rsz = (int)(frames * ChannelCount * (srcFloat ? 4 : 2));
				int actual = Stream.Read(dst.Slice(0, rsz));
				if (actual != rsz)
					throw new IncompleteDataException("WAVE read", (uint)(rsz - actual));
				return (frames, 0, false);
			}
			else
			{
				int frameSize = (srcFloat ? 4 : 2) * (int)ChannelCount;
				int tmpSize = frameSize * 128; // Buffer 128 frames at a time
				Span<byte> tmp = stackalloc byte[tmpSize];
				int dstOff = 0;
				uint rem = frames;

				while (rem > 0)
				{
					int read = Math.Min((int)rem, 128);
					int readBytes = read * frameSize;
					var readTmp = tmp.Slice(0, readBytes);
					
					int actual = Stream.Read(readTmp);
					if (actual != readBytes)
						throw new IncompleteDataException("WAVE read", (uint)(readBytes - actual));

					if (dstFloat)
						_codec.Decode(readTmp, dst.Slice(dstOff, readBytes).UnsafeCast<float>());
					else
						_codec.Decode(readTmp, dst.Slice(dstOff, readBytes).UnsafeCast<short>());

					rem -= (uint)read;
					dstOff += readBytes;
				}

				return (frames, 0, false);
			}
		}

		protected override void OnDispose(bool disposing) { }
	}
}
