/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL
{
	/// <summary>
	/// Exception produced when a data stream does not provide enough data to make a complete header.
	/// </summary>
	public sealed class IncompleteHeaderException : Exception
	{
		/// <summary>
		/// The type or name of the header that is incomplete.
		/// </summary>
		public readonly string HeaderType;

		public IncompleteHeaderException(string type) :
			base($"Stream produced incomplete header type '{type}'")
		{
			HeaderType = type;
		}

		public IncompleteHeaderException(string type, string msg) :
			base(msg)
		{
			HeaderType = type;
		}

		public IncompleteHeaderException(string type, Exception inner) :
			base($"Stream produced incomplete header type '{type}'", inner)
		{
			HeaderType = type;
		}

		public IncompleteHeaderException(string type, string msg, Exception inner) :
			base(msg, inner)
		{
			HeaderType = type;
		}
	}
}
