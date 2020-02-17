/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL
{
	public sealed class UnsupportedFormatException : Exception
	{
		#region Fields
		/// <summary>
		/// The name of the unsupported format.
		/// </summary>
		public readonly string Format;
		#endregion // Fields

		/// <summary>
		/// Creates a new exception.
		/// </summary>
		/// <param name="fmt">The unsupported format name.</param>
		public UnsupportedFormatException(string fmt) :
			base($"Unsupported format: '{fmt}'")
		{
			Format = fmt;
		}

		/// <summary>
		/// Creates a new exception.
		/// </summary>
		/// <param name="fmt">The unsupported format name.</param>
		/// <param name="msg">Custom error message.</param>
		public UnsupportedFormatException(string fmt, string msg) :
			base($"Unsupported format: '{fmt}' - {msg}")
		{
			Format = fmt;
		}
	}
}
