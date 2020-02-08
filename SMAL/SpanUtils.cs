/*
 * MIT License (MIT) - Copyright (c) 2020 SMAL Authors
 * This file is subject to the terms and conditions of the MIT License, the text of which can be found in the 'LICENSE'
 * file at the root of this repository, or online at <https://opensource.org/licenses/MIT>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SMAL
{
	/// <summary>
	/// Contains utlity functionality for working with <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/>.
	/// </summary>
	public static class SpanUtils
	{
		/// <summary>
		/// Performs a fast, but unsafe, cast from a byte span to a span of another type. This call bypasses the normal
		/// checks for references within <typeparamref name="TTo"/>.
		/// </summary>
		/// <typeparam name="TTo">The cast destination type, which <em>must not</em> contain references.</typeparam>
		/// <param name="src">The span to cast.</param>
		/// <returns>The source span reinterpreted to a new type.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<TTo> UnsafeCast<TTo>(this Span<byte> src)
			where TTo : struct
		{
			ulong length = (ulong)src.Length / (ulong)Unsafe.SizeOf<TTo>();
			return (length == 0) ? Span<TTo>.Empty : MemoryMarshal.CreateSpan(
				ref Unsafe.As<byte, TTo>(ref MemoryMarshal.GetReference(src)), (int)length);
		}

		/// <summary>
		/// Performs a fast, but unsafe, cast from a struct span to a byte span. This call bypasses the normal checks
		/// for references within <typeparamref name="TFrom"/>.
		/// </summary>
		/// <typeparam name="TFrom">The cast source type, which <em>must not</em> contain references.</typeparam>
		/// <param name="src">The span to cast.</param>
		/// <returns>The source span reinterpreted to a byte span.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<byte> AsBytesUnsafe<TFrom>(this Span<TFrom> src)
			where TFrom : struct
		{
			ulong length = (ulong)Unsafe.SizeOf<TFrom>() * (ulong)src.Length;
			return (length == 0) ? Span<byte>.Empty : MemoryMarshal.CreateSpan(
				ref Unsafe.As<TFrom, byte>(ref MemoryMarshal.GetReference(src)), (int)length);
		}
	}
}
