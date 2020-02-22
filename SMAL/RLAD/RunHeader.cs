/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SMAL.Rlad
{
	/// <summary>
	/// Contains the one byte header for an RLAD sample run.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size=1)]
	public struct RunHeader
	{
		#region Fields
		/// <summary>
		/// The raw value of the header, bit packed as (ttssssss).
		/// </summary>
		[FieldOffset(0)]
		public byte Value;

		/// <summary>
		/// The type of the run (0 = tiny, 1 = small, 2 = medium, 3 = full).
		/// </summary>
		public readonly int Type => Value >> 6;
		/// <summary>
		/// The total number of chunks (8-sample groups) in the run.
		/// </summary>
		public readonly int Count => (Value & 0x3F) + 1;
		/// <summary>
		/// The total number of samples in the run (chunk count * 8).
		/// </summary>
		public readonly int TotalSamples => ((Value & 0x3F) + 1) * 8;
		#endregion // Fields

		/// <summary>
		/// Creates a new header from the given type and chunk count.
		/// </summary>
		/// <param name="type">The chunk type.</param>
		/// <param name="extra">The extra chunk count.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public RunHeader(int type, int extra) =>
			Value = (byte)(((type & 0x03) << 6) | (extra & 0x3F));

		/// <summary>
		/// Creates a new header from a raw value.
		/// </summary>
		/// <param name="value">The raw header value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public RunHeader(byte value) => Value = value;

		/// <summary>
		/// Implicit conversion from a byte value to a run header.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator RunHeader (byte value) => new RunHeader(value);

		public override string ToString() => $"{{T:{Type} C:{Count}}}";

		public override bool Equals(object obj) => (obj is RunHeader o) && (o.Value == Value);

		public override int GetHashCode() => Value.GetHashCode();

		public static bool operator == (RunHeader l, RunHeader r) => l.Value == r.Value;
		public static bool operator != (RunHeader l, RunHeader r) => l.Value != r.Value;
	}
}
