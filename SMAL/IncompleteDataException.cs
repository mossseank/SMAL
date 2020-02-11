/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;

namespace SMAL
{
	/// <summary>
	/// Exception produced when a data source does not provide enough data to complete a decode or encode operation.
	/// </summary>
	public class IncompleteDataException : Exception
	{
		#region Fields
		/// <summary>
		/// A description of the operation that failed.
		/// </summary>
		public readonly string Operation;
		/// <summary>
		/// The discrepancy (in bytes) between the expected size, and actual size, if known.
		/// </summary>
		public readonly uint Remainder;
		#endregion // Fields

		public IncompleteDataException(string op, uint rem) :
			base($"Not enough data for operation ({op})")
		{
			Operation = op;
			Remainder = rem;
		}

		public IncompleteDataException(string op, uint rem, string msg) :
			base(msg)
		{
			Operation = op;
			Remainder = rem;
		}

		public IncompleteDataException(string op, uint rem, Exception inner) :
			base($"Not enough data for operation ({op})", inner)
		{
			Operation = op;
			Remainder = rem;
		}

		public IncompleteDataException(string op, uint rem, string msg, Exception inner) :
			base(msg, inner)
		{
			Operation = op;
			Remainder = rem;
		}
	}
}
