/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL
{
	/// <summary>
	/// Exception produced when a data stream does not provide data for a whole-number audio frame count.
	/// </summary>
	public class IncompleteFrameException : Exception
	{
		#region Fields
		/// <summary>
		/// The encoding of the expected data.
		/// </summary>
		public readonly AudioEncoding Encoding;
		/// <summary>
		/// The channels of the expected data.
		/// </summary>
		public readonly AudioChannels Channels;
		/// <summary>
		/// The discrepancy (in bytes) between the frame size and actual data size.
		/// </summary>
		public readonly uint Remainder;
		#endregion // Fields

		public IncompleteFrameException(AudioEncoding enc, AudioChannels chan, uint rem) :
			base("Data did not procduce a whole number of audio frames")
		{
			Encoding = enc;
			Channels = chan;
			Remainder = rem;
		}

		public IncompleteFrameException(AudioEncoding enc, AudioChannels chan, uint rem, string msg) :
			base(msg)
		{
			Encoding = enc;
			Channels = chan;
			Remainder = rem;
		}
	}
}
