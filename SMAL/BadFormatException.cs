/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL
{
	/// <summary>
	/// Produced when a file or stream does not match the expected format.
	/// </summary>
	public sealed class BadFormatException : Exception
	{
		#region Fields
		/// <summary>
		/// The name of the format that was expected for the source.
		/// </summary>
		public readonly string ExpectedFormat;
		#endregion // Fields

		/// <summary>
		/// Creates a new exception.
		/// </summary>
		/// <param name="exfmt">The expected format.</param>
		public BadFormatException(string exfmt) :
			base($"Data was not expected format '{exfmt}'")
		{
			ExpectedFormat = exfmt;
		}

		/// <summary>
		/// Creates a new exception.
		/// </summary>
		/// <param name="exfmt">The expected format.</param>
		/// <param name="msg">Custom error message.</param>
		public BadFormatException(string exfmt, string msg) :
			base($"Data was not expected format '{exfmt}' - {msg}")
		{
			ExpectedFormat = exfmt;
		}
	}
}
