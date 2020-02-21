﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace NBitcoin.Secp256k1
{
	public readonly struct FieldElement : IEquatable<FieldElement>
	{
		readonly uint n0, n1, n2, n3, n4, n5, n6, n7, n8, n9;
		readonly int magnitude;
		readonly bool normalized;

		static readonly FieldElement _Zero = new FieldElement(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		public static ref readonly FieldElement Zero => ref _Zero;

		public FieldElement(uint a)
		{
			n0 = a;
			n1 = n2 = n3 = n4 = n5 = n6 = n7 = n8 = n9 = 0;
			magnitude = 1;
			normalized = true;
			VERIFY();
		}
		public FieldElement(ReadOnlySpan<byte> bytes)
		{
			n0 = (uint)bytes[31] | ((uint)bytes[30] << 8) | ((uint)bytes[29] << 16) | ((uint)(bytes[28] & 0x3) << 24);
			n1 = (uint)((bytes[28] >> 2) & 0x3f) | ((uint)bytes[27] << 6) | ((uint)bytes[26] << 14) | ((uint)(bytes[25] & 0xf) << 22);
			n2 = (uint)((bytes[25] >> 4) & 0xf) | ((uint)bytes[24] << 4) | ((uint)bytes[23] << 12) | ((uint)(bytes[22] & 0x3f) << 20);
			n3 = (uint)((bytes[22] >> 6) & 0x3) | ((uint)bytes[21] << 2) | ((uint)bytes[20] << 10) | ((uint)bytes[19] << 18);
			n4 = (uint)bytes[18] | ((uint)bytes[17] << 8) | ((uint)bytes[16] << 16) | ((uint)(bytes[15] & 0x3) << 24);
			n5 = (uint)((bytes[15] >> 2) & 0x3f) | ((uint)bytes[14] << 6) | ((uint)bytes[13] << 14) | ((uint)(bytes[12] & 0xf) << 22);
			n6 = (uint)((bytes[12] >> 4) & 0xf) | ((uint)bytes[11] << 4) | ((uint)bytes[10] << 12) | ((uint)(bytes[9] & 0x3f) << 20);
			n7 = (uint)((bytes[9] >> 6) & 0x3) | ((uint)bytes[8] << 2) | ((uint)bytes[7] << 10) | ((uint)bytes[6] << 18);
			n8 = (uint)bytes[5] | ((uint)bytes[4] << 8) | ((uint)bytes[3] << 16) | ((uint)(bytes[2] & 0x3) << 24);
			n9 = (uint)((bytes[2] >> 2) & 0x3f) | ((uint)bytes[1] << 6) | ((uint)bytes[0] << 14);
			if (n9 == 0x3FFFFFUL && (n8 & n7 & n6 & n5 & n4 & n3 & n2) == 0x3FFFFFFUL && (n1 + 0x40UL + ((n0 + 0x3D1UL) >> 26)) > 0x3FFFFFFUL)
			{
				throw new ArgumentException(paramName: nameof(bytes), message: "Invalid Field");
			}
			magnitude = 1;
			normalized = true;
			VERIFY();
		}
		public FieldElement(uint n0, uint n1, uint n2, uint n3, uint n4, uint n5, uint n6, uint n7, uint n8, uint n9)
		{
			this.n0 = n0;
			this.n1 = n1;
			this.n2 = n2;
			this.n3 = n3;
			this.n4 = n4;
			this.n5 = n5;
			this.n6 = n6;
			this.n7 = n7;
			this.n8 = n8;
			this.n9 = n9;
			if (n9 == 0x3FFFFFUL && (n8 & n7 & n6 & n5 & n4 & n3 & n2) == 0x3FFFFFFUL && (n1 + 0x40UL + ((n0 + 0x3D1UL) >> 26)) > 0x3FFFFFFUL)
			{
				throw new ArgumentException(paramName: "n", message: "Invalid Field");
			}
			magnitude = 1;
			normalized = true;
			VERIFY();
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly bool Sqrt(out FieldElement result)
		{
			ref readonly FieldElement a = ref this;
			var (n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized) = this;
			/** Given that p is congruent to 3 mod 4, we can compute the square root of
			 *  a mod p as the (p+1)/4'th power of a.
			 *
			 *  As (p+1)/4 is an even number, it will have the same result for a and for
			 *  (-a). Only one of these two numbers actually has a square root however,
			 *  so we test at the end by squaring and comparing to the input.
			 *  Also because (p+1)/4 is an even number, the computed square root is
			 *  itself always a square (a ** ((p+1)/4) is the square of a ** ((p+1)/8)).
			 */
			FieldElement x2, x3, x6, x9, x11, x22, x44, x88, x176, x220, x223, t1;
			int j;

			/** The binary representation of (p + 1)/4 has 3 blocks of 1s, with lengths in
			 *  { 2, 22, 223 }. Use an addition chain to calculate 2^n - 1 for each block:
			 *  1, [2], 3, 6, 9, 11, [22], 44, 88, 176, 220, [223]
			 */

			x2 = a.Sqr();
			x2 = x2 * a;

			x3 = x2.Sqr();
			x3 = x3 * a;

			x6 = x3;
			for (j = 0; j < 3; j++)
			{
				x6 = x6.Sqr();
			}
			x6 = x6 * x3;

			x9 = x6;
			for (j = 0; j < 3; j++)
			{
				x9 = x9.Sqr();
			}
			x9 = x9 * x3;

			x11 = x9;
			for (j = 0; j < 2; j++)
			{
				x11 = x11.Sqr();
			}
			x11 = x11 * x2;

			x22 = x11;
			for (j = 0; j < 11; j++)
			{
				x22 = x22.Sqr();
			}
			x22 = x22 * x11;

			x44 = x22;
			for (j = 0; j < 22; j++)
			{
				x44 = x44.Sqr();
			}
			x44 = x44 * x22;

			x88 = x44;
			for (j = 0; j < 44; j++)
			{
				x88 = x88.Sqr();
			}
			x88 = x88 * x44;

			x176 = x88;
			for (j = 0; j < 88; j++)
			{
				x176 = x176.Sqr();
			}
			x176 = x176 * x88;

			x220 = x176;
			for (j = 0; j < 44; j++)
			{
				x220 = x220.Sqr();
			}
			x220 = x220 * x44;

			x223 = x220;
			for (j = 0; j < 3; j++)
			{
				x223 = x223.Sqr();
			}
			x223 = x223 * x3;

			/* The final result is then assembled using a sliding window over the blocks. */

			t1 = x223;
			for (j = 0; j < 23; j++)
			{
				t1 = t1.Sqr();
			}
			t1 = t1 * x22;
			for (j = 0; j < 6; j++)
			{
				t1 = t1.Sqr();
			}
			t1 = t1 * x2;
			t1 = t1.Sqr();
			result = t1.Sqr();

			/* Check that a square root was actually calculated */

			t1 = result.Sqr();
			return t1.Equals(a);
		}

		public readonly FieldElementStorage ToStorage()
		{
			uint n0, n1, n2, n3, n4, n5, n6, n7;
			ref readonly FieldElement a = ref this;
			VERIFY_CHECK(a.normalized);
			n0 = a.n0 | a.n1 << 26;
			n1 = a.n1 >> 6 | a.n2 << 20;
			n2 = a.n2 >> 12 | a.n3 << 14;
			n3 = a.n3 >> 18 | a.n4 << 8;
			n4 = a.n4 >> 24 | a.n5 << 2 | a.n6 << 28;
			n5 = a.n6 >> 4 | a.n7 << 22;
			n6 = a.n7 >> 10 | a.n8 << 16;
			n7 = a.n8 >> 16 | a.n9 << 10;
			return new FieldElementStorage(n0, n1, n2, n3, n4, n5, n6, n7);
		}
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly FieldElement Inverse()
		{
			FieldElement x2, x3, x6, x9, x11, x22, x44, x88, x176, x220, x223, t1;
			int j;
			ref readonly FieldElement a = ref this;
			/** The binary representation of (p - 2) has 5 blocks of 1s, with lengths in
			 *  { 1, 2, 22, 223 }. Use an addition chain to calculate 2^n - 1 for each block:
			 *  [1], [2], 3, 6, 9, 11, [22], 44, 88, 176, 220, [223]
			 */

			x2 = a.Sqr();
			x2 = x2 * a;

			x3 = x2.Sqr();
			x3 = x3 * a;

			x6 = x3;
			for (j = 0; j < 3; j++)
			{
				x6 = x6.Sqr();
			}
			x6 = x6 * x3;

			x9 = x6;
			for (j = 0; j < 3; j++)
			{
				x9 = x9.Sqr();
			}
			x9 = x9 * x3;

			x11 = x9;
			for (j = 0; j < 2; j++)
			{
				x11 = x11.Sqr();
			}
			x11 = x11 * x2;

			x22 = x11;
			for (j = 0; j < 11; j++)
			{
				x22 = x22.Sqr();
			}
			x22 = x22 * x11;

			x44 = x22;
			for (j = 0; j < 22; j++)
			{
				x44 = x44.Sqr();
			}
			x44 = x44 * x22;

			x88 = x44;
			for (j = 0; j < 44; j++)
			{
				x88 = x88.Sqr();
			}
			x88 = x88 * x44;

			x176 = x88;
			for (j = 0; j < 88; j++)
			{
				x176 = x176.Sqr();
			}
			x176 = x176 * x88;

			x220 = x176;
			for (j = 0; j < 44; j++)
			{
				x220 = x220.Sqr();
			}
			x220 = x220 * x44;

			x223 = x220;
			for (j = 0; j < 3; j++)
			{
				x223 = x223.Sqr();
			}
			x223 = x223 * x3;

			/* The final result is then assembled using a sliding window over the blocks. */

			t1 = x223;
			for (j = 0; j < 23; j++)
			{
				t1 = t1.Sqr();
			}
			t1 = t1 * x22;
			for (j = 0; j < 5; j++)
			{
				t1 = t1.Sqr();
			}
			t1 = t1 * a;
			for (j = 0; j < 3; j++)
			{
				t1 = t1.Sqr();
			}
			t1 = t1 * x2;
			for (j = 0; j < 2; j++)
			{
				t1 = t1.Sqr();
			}
			return a * t1;
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly FieldElement Sqr()
		{
			var (n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, _, _) = Zero;
			int magnitude;
			bool normalized;
			VERIFY_CHECK(this.magnitude <= 8);
			VERIFY();
			secp256k1_fe_sqr_inner(ref n0, ref n1, ref n2, ref n3, ref n4, ref n5, ref n6, ref n7, ref n8, ref n9);
			magnitude = 1;
			normalized = false;
			var r = new FieldElement(n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized);
			r.VERIFY();
			return r;
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		private readonly void secp256k1_fe_sqr_inner(ref uint n0, ref uint n1, ref uint n2, ref uint n3, ref uint n4, ref uint n5, ref uint n6, ref uint n7, ref uint n8, ref uint n9)
		{
			ulong c, d;
			ulong u0, u1, u2, u3, u4, u5, u6, u7, u8;
			uint t9, t0, t1, t2, t3, t4, t5, t6, t7;
			const uint M = 0x3FFFFFFU, R0 = 0x3D10U, R1 = 0x400U;
			ref readonly FieldElement a = ref this;
			VERIFY_BITS(a.n0, 30);
			VERIFY_BITS(a.n1, 30);
			VERIFY_BITS(a.n2, 30);
			VERIFY_BITS(a.n3, 30);
			VERIFY_BITS(a.n4, 30);
			VERIFY_BITS(a.n5, 30);
			VERIFY_BITS(a.n6, 30);
			VERIFY_BITS(a.n7, 30);
			VERIFY_BITS(a.n8, 30);
			VERIFY_BITS(a.n9, 26);

			/** [... a b c] is a shorthand for ... + a<<52 + b<<26 + c<<0 mod n.
			 *  px is a shorthand for sum(a.ni*a[x-i], i=0..x).
			 *  Note that [x 0 0 0 0 0 0 0 0 0 0] = [x*R1 x*R0].
			 */

			d = (ulong)(a.n0 * 2) * a.n9
			   + (ulong)(a.n1 * 2) * a.n8
			   + (ulong)(a.n2 * 2) * a.n7
			   + (ulong)(a.n3 * 2) * a.n6
			   + (ulong)(a.n4 * 2) * a.n5;
			/* VERIFY_BITS(d, 64); */
			/* [d 0 0 0 0 0 0 0 0 0] = [p9 0 0 0 0 0 0 0 0 0] */
			t9 = (uint)(d & M); d >>= 26;
			VERIFY_BITS(t9, 26);
			VERIFY_BITS(d, 38);
			/* [d t9 0 0 0 0 0 0 0 0 0] = [p9 0 0 0 0 0 0 0 0 0] */

			c = (ulong)a.n0 * a.n0;
			VERIFY_BITS(c, 60);
			/* [d t9 0 0 0 0 0 0 0 0 c] = [p9 0 0 0 0 0 0 0 0 p0] */
			d += (ulong)(a.n1 * 2) * a.n9
			   + (ulong)(a.n2 * 2) * a.n8
			   + (ulong)(a.n3 * 2) * a.n7
			   + (ulong)(a.n4 * 2) * a.n6
			   + (ulong)a.n5 * a.n5;
			VERIFY_BITS(d, 63);
			/* [d t9 0 0 0 0 0 0 0 0 c] = [p10 p9 0 0 0 0 0 0 0 0 p0] */
			u0 = (uint)(d & M); d >>= 26; c += u0 * R0;
			VERIFY_BITS(u0, 26);
			VERIFY_BITS(d, 37);
			VERIFY_BITS(c, 61);
			/* [d u0 t9 0 0 0 0 0 0 0 0 c-u0*R0] = [p10 p9 0 0 0 0 0 0 0 0 p0] */
			t0 = (uint)(c & M); c >>= 26; c += u0 * R1;
			VERIFY_BITS(t0, 26);
			VERIFY_BITS(c, 37);
			/* [d u0 t9 0 0 0 0 0 0 0 c-u0*R1 t0-u0*R0] = [p10 p9 0 0 0 0 0 0 0 0 p0] */
			/* [d 0 t9 0 0 0 0 0 0 0 c t0] = [p10 p9 0 0 0 0 0 0 0 0 p0] */

			c += (ulong)(a.n0 * 2) * a.n1;
			VERIFY_BITS(c, 62);
			/* [d 0 t9 0 0 0 0 0 0 0 c t0] = [p10 p9 0 0 0 0 0 0 0 p1 p0] */
			d += (ulong)(a.n2 * 2) * a.n9
			   + (ulong)(a.n3 * 2) * a.n8
			   + (ulong)(a.n4 * 2) * a.n7
			   + (ulong)(a.n5 * 2) * a.n6;
			VERIFY_BITS(d, 63);
			/* [d 0 t9 0 0 0 0 0 0 0 c t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */
			u1 = (uint)(d & M); d >>= 26; c += u1 * R0;
			VERIFY_BITS(u1, 26);
			VERIFY_BITS(d, 37);
			VERIFY_BITS(c, 63);
			/* [d u1 0 t9 0 0 0 0 0 0 0 c-u1*R0 t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */
			t1 = (uint)(c & M); c >>= 26; c += u1 * R1;
			VERIFY_BITS(t1, 26);
			VERIFY_BITS(c, 38);
			/* [d u1 0 t9 0 0 0 0 0 0 c-u1*R1 t1-u1*R0 t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */
			/* [d 0 0 t9 0 0 0 0 0 0 c t1 t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */

			c += (ulong)(a.n0 * 2) * a.n2
			   + (ulong)a.n1 * a.n1;
			VERIFY_BITS(c, 62);
			/* [d 0 0 t9 0 0 0 0 0 0 c t1 t0] = [p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
			d += (ulong)(a.n3 * 2) * a.n9
			   + (ulong)(a.n4 * 2) * a.n8
			   + (ulong)(a.n5 * 2) * a.n7
			   + (ulong)a.n6 * a.n6;
			VERIFY_BITS(d, 63);
			/* [d 0 0 t9 0 0 0 0 0 0 c t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
			u2 = (uint)(d & M); d >>= 26; c += u2 * R0;
			VERIFY_BITS(u2, 26);
			VERIFY_BITS(d, 37);
			VERIFY_BITS(c, 63);
			/* [d u2 0 0 t9 0 0 0 0 0 0 c-u2*R0 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
			t2 = (uint)(c & M); c >>= 26; c += u2 * R1;
			VERIFY_BITS(t2, 26);
			VERIFY_BITS(c, 38);
			/* [d u2 0 0 t9 0 0 0 0 0 c-u2*R1 t2-u2*R0 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
			/* [d 0 0 0 t9 0 0 0 0 0 c t2 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */

			c += (ulong)(a.n0 * 2) * a.n3
			   + (ulong)(a.n1 * 2) * a.n2;
			VERIFY_BITS(c, 63);
			/* [d 0 0 0 t9 0 0 0 0 0 c t2 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
			d += (ulong)(a.n4 * 2) * a.n9
			   + (ulong)(a.n5 * 2) * a.n8
			   + (ulong)(a.n6 * 2) * a.n7;
			VERIFY_BITS(d, 63);
			/* [d 0 0 0 t9 0 0 0 0 0 c t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
			u3 = (uint)(d & M); d >>= 26; c += u3 * R0;
			VERIFY_BITS(u3, 26);
			VERIFY_BITS(d, 37);
			/* VERIFY_BITS(c, 64); */
			/* [d u3 0 0 0 t9 0 0 0 0 0 c-u3*R0 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
			t3 = (uint)(c & M); c >>= 26; c += u3 * R1;
			VERIFY_BITS(t3, 26);
			VERIFY_BITS(c, 39);
			/* [d u3 0 0 0 t9 0 0 0 0 c-u3*R1 t3-u3*R0 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
			/* [d 0 0 0 0 t9 0 0 0 0 c t3 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */

			c += (ulong)(a.n0 * 2) * a.n4
			   + (ulong)(a.n1 * 2) * a.n3
			   + (ulong)a.n2 * a.n2;
			VERIFY_BITS(c, 63);
			/* [d 0 0 0 0 t9 0 0 0 0 c t3 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
			d += (ulong)(a.n5 * 2) * a.n9
			   + (ulong)(a.n6 * 2) * a.n8
			   + (ulong)a.n7 * a.n7;
			VERIFY_BITS(d, 62);
			/* [d 0 0 0 0 t9 0 0 0 0 c t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
			u4 = (uint)(d & M); d >>= 26; c += u4 * R0;
			VERIFY_BITS(u4, 26);
			VERIFY_BITS(d, 36);
			/* VERIFY_BITS(c, 64); */
			/* [d u4 0 0 0 0 t9 0 0 0 0 c-u4*R0 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
			t4 = (uint)(c & M); c >>= 26; c += u4 * R1;
			VERIFY_BITS(t4, 26);
			VERIFY_BITS(c, 39);
			/* [d u4 0 0 0 0 t9 0 0 0 c-u4*R1 t4-u4*R0 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 t9 0 0 0 c t4 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */

			c += (ulong)(a.n0 * 2) * a.n5
			   + (ulong)(a.n1 * 2) * a.n4
			   + (ulong)(a.n2 * 2) * a.n3;
			VERIFY_BITS(c, 63);
			/* [d 0 0 0 0 0 t9 0 0 0 c t4 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
			d += (ulong)(a.n6 * 2) * a.n9
			   + (ulong)(a.n7 * 2) * a.n8;
			VERIFY_BITS(d, 62);
			/* [d 0 0 0 0 0 t9 0 0 0 c t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
			u5 = (uint)(d & M); d >>= 26; c += u5 * R0;
			VERIFY_BITS(u5, 26);
			VERIFY_BITS(d, 36);
			/* VERIFY_BITS(c, 64); */
			/* [d u5 0 0 0 0 0 t9 0 0 0 c-u5*R0 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
			t5 = (uint)(c & M); c >>= 26; c += u5 * R1;
			VERIFY_BITS(t5, 26);
			VERIFY_BITS(c, 39);
			/* [d u5 0 0 0 0 0 t9 0 0 c-u5*R1 t5-u5*R0 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 t9 0 0 c t5 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */

			c += (ulong)(a.n0 * 2) * a.n6
			   + (ulong)(a.n1 * 2) * a.n5
			   + (ulong)(a.n2 * 2) * a.n4
			   + (ulong)a.n3 * a.n3;
			VERIFY_BITS(c, 63);
			/* [d 0 0 0 0 0 0 t9 0 0 c t5 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
			d += (ulong)(a.n7 * 2) * a.n9
			   + (ulong)a.n8 * a.n8;
			VERIFY_BITS(d, 61);
			/* [d 0 0 0 0 0 0 t9 0 0 c t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
			u6 = (uint)(d & M); d >>= 26; c += u6 * R0;
			VERIFY_BITS(u6, 26);
			VERIFY_BITS(d, 35);
			/* VERIFY_BITS(c, 64); */
			/* [d u6 0 0 0 0 0 0 t9 0 0 c-u6*R0 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
			t6 = (uint)(c & M); c >>= 26; c += u6 * R1;
			VERIFY_BITS(t6, 26);
			VERIFY_BITS(c, 39);
			/* [d u6 0 0 0 0 0 0 t9 0 c-u6*R1 t6-u6*R0 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 0 t9 0 c t6 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */

			c += (ulong)(a.n0 * 2) * a.n7
			   + (ulong)(a.n1 * 2) * a.n6
			   + (ulong)(a.n2 * 2) * a.n5
			   + (ulong)(a.n3 * 2) * a.n4;
			/* VERIFY_BITS(c, 64); */
			VERIFY_CHECK(c <= 0x8000007C00000007UL);
			/* [d 0 0 0 0 0 0 0 t9 0 c t6 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
			d += (ulong)(a.n8 * 2) * a.n9;
			VERIFY_BITS(d, 58);
			/* [d 0 0 0 0 0 0 0 t9 0 c t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
			u7 = (uint)(d & M); d >>= 26; c += u7 * R0;
			VERIFY_BITS(u7, 26);
			VERIFY_BITS(d, 32);
			/* VERIFY_BITS(c, 64); */
			VERIFY_CHECK(c <= 0x800001703FFFC2F7UL);
			/* [d u7 0 0 0 0 0 0 0 t9 0 c-u7*R0 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
			t7 = (uint)(c & M); c >>= 26; c += u7 * R1;
			VERIFY_BITS(t7, 26);
			VERIFY_BITS(c, 38);
			/* [d u7 0 0 0 0 0 0 0 t9 c-u7*R1 t7-u7*R0 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 0 0 t9 c t7 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */

			c += (ulong)(a.n0 * 2) * a.n8
			   + (ulong)(a.n1 * 2) * a.n7
			   + (ulong)(a.n2 * 2) * a.n6
			   + (ulong)(a.n3 * 2) * a.n5
			   + (ulong)a.n4 * a.n4;
			/* VERIFY_BITS(c, 64); */
			VERIFY_CHECK(c <= 0x9000007B80000008UL);
			/* [d 0 0 0 0 0 0 0 0 t9 c t7 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			d += (ulong)a.n9 * a.n9;
			VERIFY_BITS(d, 57);
			/* [d 0 0 0 0 0 0 0 0 t9 c t7 t6 t5 t4 t3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			u8 = (uint)(d & M); d >>= 26; c += u8 * R0;
			VERIFY_BITS(u8, 26);
			VERIFY_BITS(d, 31);
			/* VERIFY_BITS(c, 64); */
			VERIFY_CHECK(c <= 0x9000016FBFFFC2F8UL);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 t5 t4 t3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */

			n3 = t3;
			VERIFY_BITS(n3, 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 t5 t4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n4 = t4;
			VERIFY_BITS(n4, 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 t5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n5 = t5;
			VERIFY_BITS(n5, 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n6 = t6;
			VERIFY_BITS(n6, 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n7 = t7;
			VERIFY_BITS(n7, 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */

			n8 = (uint)(c & M); c >>= 26; c += u8 * R1;
			VERIFY_BITS(n8, 26);
			VERIFY_BITS(c, 39);
			/* [d u8 0 0 0 0 0 0 0 0 t9+c-u8*R1 r8-u8*R0 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 0 0 0 t9+c r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			c += d * R0 + t9;
			VERIFY_BITS(c, 45);
			/* [d 0 0 0 0 0 0 0 0 0 c-d*R0 r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n9 = (uint)(c & (M >> 4)); c >>= 22; c += d * (R1 << 4);
			VERIFY_BITS(n9, 22);
			VERIFY_BITS(c, 46);
			/* [d 0 0 0 0 0 0 0 0 r9+((c-d*R1<<4)<<22)-d*R0 r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 0 -d*R1 r9+(c<<22)-d*R0 r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */

			d = c * (R0 >> 4) + t0;
			VERIFY_BITS(d, 56);
			/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 t1 d-c*R0>>4] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n0 = (uint)(d & M); d >>= 26;
			VERIFY_BITS(n0, 26);
			VERIFY_BITS(d, 30);
			/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 t1+d r0-c*R0>>4] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			d += c * (R1 >> 4) + t1;
			VERIFY_BITS(d, 53);
			VERIFY_CHECK(d <= 0x10000003FFFFBFUL);
			/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 d-c*R1>>4 r0-c*R0>>4] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [r9 r8 r7 r6 r5 r4 r3 t2 d r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n1 = (uint)(d & M); d >>= 26;
			VERIFY_BITS(n1, 26);
			VERIFY_BITS(d, 27);
			VERIFY_CHECK(d <= 0x4000000UL);
			/* [r9 r8 r7 r6 r5 r4 r3 t2+d r1 r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			d += t2;
			VERIFY_BITS(d, 27);
			/* [r9 r8 r7 r6 r5 r4 r3 d r1 r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n2 = (uint)d;
			VERIFY_BITS(n2, 27);
			/* [r9 r8 r7 r6 r5 r4 r3 r2 r1 r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
		}

		private void VERIFY_BITS(ulong x, int n)
		{
			VERIFY_CHECK(((x) >> (n)) == 0);
		}
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly FieldElement Multiply(in FieldElement b)
		{
			var (n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, _, _) = Zero;
			int magnitude;
			bool normalized;
			VERIFY_CHECK(this.magnitude <= 8);
			VERIFY();
			VERIFY_CHECK(b.magnitude <= 8);
			b.VERIFY();
			secp256k1_fe_mul_inner(ref n0, ref n1, ref n2, ref n3, ref n4, ref n5, ref n6, ref n7, ref n8, ref n9, b);
			magnitude = 1;
			normalized = false;
			var r = new FieldElement(n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized);
			r.VERIFY();
			return r;
		}
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly FieldElement Multiply(uint a)
		{
			var (n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized) = this;
			n0 *= a;
			n1 *= a;
			n2 *= a;
			n3 *= a;
			n4 *= a;
			n5 *= a;
			n6 *= a;
			n7 *= a;
			n8 *= a;
			n9 *= a;
			magnitude *= (int)a;
			normalized = false;
			var r = new FieldElement(n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized);
			r.VERIFY();
			return r;
		}
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly FieldElement Add(in FieldElement a)
		{
			var (n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized) = this;
			a.VERIFY();
			n0 += a.n0;
			n1 += a.n1;
			n2 += a.n2;
			n3 += a.n3;
			n4 += a.n4;
			n5 += a.n5;
			n6 += a.n6;
			n7 += a.n7;
			n8 += a.n8;
			n9 += a.n9;
			magnitude += a.magnitude;
			normalized = false;
			var r = new FieldElement(n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized);
			r.VERIFY();
			return r;
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		private readonly void secp256k1_fe_mul_inner(ref uint n0, ref uint n1, ref uint n2, ref uint n3, ref uint n4, ref uint n5, ref uint n6, ref uint n7, ref uint n8, ref uint n9, in FieldElement b)
		{
			ref readonly FieldElement a = ref this;
			ulong c, d;
			ulong u0, u1, u2, u3, u4, u5, u6, u7, u8;
			uint t9, t1, t0, t2, t3, t4, t5, t6, t7;
			const uint M = 0x3FFFFFFU, R0 = 0x3D10U, R1 = 0x400U;

			VERIFY_BITS(a.n0, 30);
			VERIFY_BITS(a.n1, 30);
			VERIFY_BITS(a.n2, 30);
			VERIFY_BITS(a.n3, 30);
			VERIFY_BITS(a.n4, 30);
			VERIFY_BITS(a.n5, 30);
			VERIFY_BITS(a.n6, 30);
			VERIFY_BITS(a.n7, 30);
			VERIFY_BITS(a.n8, 30);
			VERIFY_BITS(a.n9, 26);
			VERIFY_BITS(b.n0, 30);
			VERIFY_BITS(b.n1, 30);
			VERIFY_BITS(b.n2, 30);
			VERIFY_BITS(b.n3, 30);
			VERIFY_BITS(b.n4, 30);
			VERIFY_BITS(b.n5, 30);
			VERIFY_BITS(b.n6, 30);
			VERIFY_BITS(b.n7, 30);
			VERIFY_BITS(b.n8, 30);
			VERIFY_BITS(b.n9, 26);

			/** [... a b c] is a shorthand for ... + a<<52 + b<<26 + c<<0 mod n.
			 *  px is a shorthand for sum(a.ni*b[x-i], i=0..x).
			 *  Note that [x 0 0 0 0 0 0 0 0 0 0] = [x*R1 x*R0].
			 */

			d = (ulong)a.n0 * b.n9
			   + (ulong)a.n1 * b.n8
			   + (ulong)a.n2 * b.n7
			   + (ulong)a.n3 * b.n6
			   + (ulong)a.n4 * b.n5
			   + (ulong)a.n5 * b.n4
			   + (ulong)a.n6 * b.n3
			   + (ulong)a.n7 * b.n2
			   + (ulong)a.n8 * b.n1
			   + (ulong)a.n9 * b.n0;
			/* VERIFY_BITS(d, 64); */
			/* [d 0 0 0 0 0 0 0 0 0] = [p9 0 0 0 0 0 0 0 0 0] */
			t9 = (uint)(d & M); d >>= 26;
			VERIFY_BITS(t9, 26);
			VERIFY_BITS(d, 38);
			/* [d t9 0 0 0 0 0 0 0 0 0] = [p9 0 0 0 0 0 0 0 0 0] */

			c = (ulong)a.n0 * b.n0;
			VERIFY_BITS(c, 60);
			/* [d t9 0 0 0 0 0 0 0 0 c] = [p9 0 0 0 0 0 0 0 0 p0] */
			d += (ulong)a.n1 * b.n9
			   + (ulong)a.n2 * b.n8
			   + (ulong)a.n3 * b.n7
			   + (ulong)a.n4 * b.n6
			   + (ulong)a.n5 * b.n5
			   + (ulong)a.n6 * b.n4
			   + (ulong)a.n7 * b.n3
			   + (ulong)a.n8 * b.n2
			   + (ulong)a.n9 * b.n1;
			VERIFY_BITS(d, 63);
			/* [d t9 0 0 0 0 0 0 0 0 c] = [p10 p9 0 0 0 0 0 0 0 0 p0] */
			u0 = (uint)(d & M); d >>= 26; c += u0 * R0;
			VERIFY_BITS(u0, 26);
			VERIFY_BITS(d, 37);
			VERIFY_BITS(c, 61);
			/* [d u0 t9 0 0 0 0 0 0 0 0 c-u0*R0] = [p10 p9 0 0 0 0 0 0 0 0 p0] */
			t0 = (uint)(c & M); c >>= 26; c += u0 * R1;
			VERIFY_BITS(t0, 26);
			VERIFY_BITS(c, 37);
			/* [d u0 t9 0 0 0 0 0 0 0 c-u0*R1 t0-u0*R0] = [p10 p9 0 0 0 0 0 0 0 0 p0] */
			/* [d 0 t9 0 0 0 0 0 0 0 c t0] = [p10 p9 0 0 0 0 0 0 0 0 p0] */

			c += (ulong)a.n0 * b.n1
			   + (ulong)a.n1 * b.n0;
			VERIFY_BITS(c, 62);
			/* [d 0 t9 0 0 0 0 0 0 0 c t0] = [p10 p9 0 0 0 0 0 0 0 p1 p0] */
			d += (ulong)a.n2 * b.n9
			   + (ulong)a.n3 * b.n8
			   + (ulong)a.n4 * b.n7
			   + (ulong)a.n5 * b.n6
			   + (ulong)a.n6 * b.n5
			   + (ulong)a.n7 * b.n4
			   + (ulong)a.n8 * b.n3
			   + (ulong)a.n9 * b.n2;
			VERIFY_BITS(d, 63);
			/* [d 0 t9 0 0 0 0 0 0 0 c t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */
			u1 = (uint)(d & M); d >>= 26; c += u1 * R0;
			VERIFY_BITS(u1, 26);
			VERIFY_BITS(d, 37);
			VERIFY_BITS(c, 63);
			/* [d u1 0 t9 0 0 0 0 0 0 0 c-u1*R0 t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */
			t1 = (uint)(c & M); c >>= 26; c += u1 * R1;
			VERIFY_BITS(t1, 26);
			VERIFY_BITS(c, 38);
			/* [d u1 0 t9 0 0 0 0 0 0 c-u1*R1 t1-u1*R0 t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */
			/* [d 0 0 t9 0 0 0 0 0 0 c t1 t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */

			c += (ulong)a.n0 * b.n2
			   + (ulong)a.n1 * b.n1
			   + (ulong)a.n2 * b.n0;
			VERIFY_BITS(c, 62);
			/* [d 0 0 t9 0 0 0 0 0 0 c t1 t0] = [p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
			d += (ulong)a.n3 * b.n9
			   + (ulong)a.n4 * b.n8
			   + (ulong)a.n5 * b.n7
			   + (ulong)a.n6 * b.n6
			   + (ulong)a.n7 * b.n5
			   + (ulong)a.n8 * b.n4
			   + (ulong)a.n9 * b.n3;
			VERIFY_BITS(d, 63);
			/* [d 0 0 t9 0 0 0 0 0 0 c t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
			u2 = (uint)(d & M); d >>= 26; c += u2 * R0;
			VERIFY_BITS(u2, 26);
			VERIFY_BITS(d, 37);
			VERIFY_BITS(c, 63);
			/* [d u2 0 0 t9 0 0 0 0 0 0 c-u2*R0 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
			t2 = (uint)(c & M); c >>= 26; c += u2 * R1;
			VERIFY_BITS(t2, 26);
			VERIFY_BITS(c, 38);
			/* [d u2 0 0 t9 0 0 0 0 0 c-u2*R1 t2-u2*R0 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
			/* [d 0 0 0 t9 0 0 0 0 0 c t2 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */

			c += (ulong)a.n0 * b.n3
			   + (ulong)a.n1 * b.n2
			   + (ulong)a.n2 * b.n1
			   + (ulong)a.n3 * b.n0;
			VERIFY_BITS(c, 63);
			/* [d 0 0 0 t9 0 0 0 0 0 c t2 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
			d += (ulong)a.n4 * b.n9
			   + (ulong)a.n5 * b.n8
			   + (ulong)a.n6 * b.n7
			   + (ulong)a.n7 * b.n6
			   + (ulong)a.n8 * b.n5
			   + (ulong)a.n9 * b.n4;
			VERIFY_BITS(d, 63);
			/* [d 0 0 0 t9 0 0 0 0 0 c t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
			u3 = (uint)(d & M); d >>= 26; c += u3 * R0;
			VERIFY_BITS(u3, 26);
			VERIFY_BITS(d, 37);
			/* VERIFY_BITS(c, 64); */
			/* [d u3 0 0 0 t9 0 0 0 0 0 c-u3*R0 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
			t3 = (uint)(c & M); c >>= 26; c += u3 * R1;
			VERIFY_BITS(t3, 26);
			VERIFY_BITS(c, 39);
			/* [d u3 0 0 0 t9 0 0 0 0 c-u3*R1 t3-u3*R0 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
			/* [d 0 0 0 0 t9 0 0 0 0 c t3 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */

			c += (ulong)a.n0 * b.n4
			   + (ulong)a.n1 * b.n3
			   + (ulong)a.n2 * b.n2
			   + (ulong)a.n3 * b.n1
			   + (ulong)a.n4 * b.n0;
			VERIFY_BITS(c, 63);
			/* [d 0 0 0 0 t9 0 0 0 0 c t3 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
			d += (ulong)a.n5 * b.n9
			   + (ulong)a.n6 * b.n8
			   + (ulong)a.n7 * b.n7
			   + (ulong)a.n8 * b.n6
			   + (ulong)a.n9 * b.n5;
			VERIFY_BITS(d, 62);
			/* [d 0 0 0 0 t9 0 0 0 0 c t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
			u4 = (uint)(d & M); d >>= 26; c += u4 * R0;
			VERIFY_BITS(u4, 26);
			VERIFY_BITS(d, 36);
			/* VERIFY_BITS(c, 64); */
			/* [d u4 0 0 0 0 t9 0 0 0 0 c-u4*R0 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
			t4 = (uint)(c & M); c >>= 26; c += u4 * R1;
			VERIFY_BITS(t4, 26);
			VERIFY_BITS(c, 39);
			/* [d u4 0 0 0 0 t9 0 0 0 c-u4*R1 t4-u4*R0 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 t9 0 0 0 c t4 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */

			c += (ulong)a.n0 * b.n5
			   + (ulong)a.n1 * b.n4
			   + (ulong)a.n2 * b.n3
			   + (ulong)a.n3 * b.n2
			   + (ulong)a.n4 * b.n1
			   + (ulong)a.n5 * b.n0;
			VERIFY_BITS(c, 63);
			/* [d 0 0 0 0 0 t9 0 0 0 c t4 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
			d += (ulong)a.n6 * b.n9
			   + (ulong)a.n7 * b.n8
			   + (ulong)a.n8 * b.n7
			   + (ulong)a.n9 * b.n6;
			VERIFY_BITS(d, 62);
			/* [d 0 0 0 0 0 t9 0 0 0 c t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
			u5 = (uint)(d & M); d >>= 26; c += u5 * R0;
			VERIFY_BITS(u5, 26);
			VERIFY_BITS(d, 36);
			/* VERIFY_BITS(c, 64); */
			/* [d u5 0 0 0 0 0 t9 0 0 0 c-u5*R0 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
			t5 = (uint)(c & M); c >>= 26; c += u5 * R1;
			VERIFY_BITS(t5, 26);
			VERIFY_BITS(c, 39);
			/* [d u5 0 0 0 0 0 t9 0 0 c-u5*R1 t5-u5*R0 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 t9 0 0 c t5 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */

			c += (ulong)a.n0 * b.n6
			   + (ulong)a.n1 * b.n5
			   + (ulong)a.n2 * b.n4
			   + (ulong)a.n3 * b.n3
			   + (ulong)a.n4 * b.n2
			   + (ulong)a.n5 * b.n1
			   + (ulong)a.n6 * b.n0;
			VERIFY_BITS(c, 63);
			/* [d 0 0 0 0 0 0 t9 0 0 c t5 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
			d += (ulong)a.n7 * b.n9
			   + (ulong)a.n8 * b.n8
			   + (ulong)a.n9 * b.n7;
			VERIFY_BITS(d, 61);
			/* [d 0 0 0 0 0 0 t9 0 0 c t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
			u6 = (uint)(d & M); d >>= 26; c += u6 * R0;
			VERIFY_BITS(u6, 26);
			VERIFY_BITS(d, 35);
			/* VERIFY_BITS(c, 64); */
			/* [d u6 0 0 0 0 0 0 t9 0 0 c-u6*R0 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
			t6 = (uint)(c & M); c >>= 26; c += u6 * R1;
			VERIFY_BITS(t6, 26);
			VERIFY_BITS(c, 39);
			/* [d u6 0 0 0 0 0 0 t9 0 c-u6*R1 t6-u6*R0 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 0 t9 0 c t6 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */

			c += (ulong)a.n0 * b.n7
			   + (ulong)a.n1 * b.n6
			   + (ulong)a.n2 * b.n5
			   + (ulong)a.n3 * b.n4
			   + (ulong)a.n4 * b.n3
			   + (ulong)a.n5 * b.n2
			   + (ulong)a.n6 * b.n1
			   + (ulong)a.n7 * b.n0;
			/* VERIFY_BITS(c, 64); */
			VERIFY_CHECK(c <= 0x8000007C00000007UL);
			/* [d 0 0 0 0 0 0 0 t9 0 c t6 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
			d += (ulong)a.n8 * b.n9
			   + (ulong)a.n9 * b.n8;
			VERIFY_BITS(d, 58);
			/* [d 0 0 0 0 0 0 0 t9 0 c t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
			u7 = (uint)(d & M); d >>= 26; c += u7 * R0;
			VERIFY_BITS(u7, 26);
			VERIFY_BITS(d, 32);
			/* VERIFY_BITS(c, 64); */
			VERIFY_CHECK(c <= 0x800001703FFFC2F7UL);
			/* [d u7 0 0 0 0 0 0 0 t9 0 c-u7*R0 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
			t7 = (uint)(c & M); c >>= 26; c += u7 * R1;
			VERIFY_BITS(t7, 26);
			VERIFY_BITS(c, 38);
			/* [d u7 0 0 0 0 0 0 0 t9 c-u7*R1 t7-u7*R0 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 0 0 t9 c t7 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */

			c += (ulong)a.n0 * b.n8
			   + (ulong)a.n1 * b.n7
			   + (ulong)a.n2 * b.n6
			   + (ulong)a.n3 * b.n5
			   + (ulong)a.n4 * b.n4
			   + (ulong)a.n5 * b.n3
			   + (ulong)a.n6 * b.n2
			   + (ulong)a.n7 * b.n1
			   + (ulong)a.n8 * b.n0;
			/* VERIFY_BITS(c, 64); */
			VERIFY_CHECK(c <= 0x9000007B80000008UL);
			/* [d 0 0 0 0 0 0 0 0 t9 c t7 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			d += (ulong)a.n9 * b.n9;
			VERIFY_BITS(d, 57);
			/* [d 0 0 0 0 0 0 0 0 t9 c t7 t6 t5 t4 t3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			u8 = (uint)(d & M); d >>= 26; c += u8 * R0;
			VERIFY_BITS(u8, 26);
			VERIFY_BITS(d, 31);
			/* VERIFY_BITS(c, 64); */
			VERIFY_CHECK(c <= 0x9000016FBFFFC2F8UL);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 t5 t4 t3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */

			n3 = t3;
			VERIFY_BITS(n3, 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 t5 t4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n4 = t4;
			VERIFY_BITS(n4, 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 t5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n5 = t5;
			VERIFY_BITS(n5, 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n6 = t6;
			VERIFY_BITS(n6, 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n7 = t7;
			VERIFY_BITS(n7, 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */

			n8 = (uint)(c & M); c >>= 26; c += u8 * R1;
			VERIFY_BITS(n8, 26);
			VERIFY_BITS(c, 39);
			/* [d u8 0 0 0 0 0 0 0 0 t9+c-u8*R1 r8-u8*R0 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 0 0 0 t9+c r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			c += d * R0 + t9;
			VERIFY_BITS(c, 45);
			/* [d 0 0 0 0 0 0 0 0 0 c-d*R0 r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n9 = (uint)(c & (M >> 4)); c >>= 22; c += d * (R1 << 4);
			VERIFY_BITS(n9, 22);
			VERIFY_BITS(c, 46);
			/* [d 0 0 0 0 0 0 0 0 r9+((c-d*R1<<4)<<22)-d*R0 r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 0 -d*R1 r9+(c<<22)-d*R0 r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */

			d = c * (R0 >> 4) + t0;
			VERIFY_BITS(d, 56);
			/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 t1 d-c*R0>>4] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n0 = (uint)(d & M); d >>= 26;
			VERIFY_BITS(n0, 26);
			VERIFY_BITS(d, 30);
			/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 t1+d r0-c*R0>>4] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			d += c * (R1 >> 4) + t1;
			VERIFY_BITS(d, 53);
			VERIFY_CHECK(d <= 0x10000003FFFFBFUL);
			/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 d-c*R1>>4 r0-c*R0>>4] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [r9 r8 r7 r6 r5 r4 r3 t2 d r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n1 = (uint)(d & M); d >>= 26;
			VERIFY_BITS(n1, 26);
			VERIFY_BITS(d, 27);
			VERIFY_CHECK(d <= 0x4000000UL);
			/* [r9 r8 r7 r6 r5 r4 r3 t2+d r1 r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			d += t2;
			VERIFY_BITS(d, 27);
			/* [r9 r8 r7 r6 r5 r4 r3 d r1 r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n2 = (uint)d;
			VERIFY_BITS(n2, 27);
			/* [r9 r8 r7 r6 r5 r4 r3 r2 r1 r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
		}

		public static bool TryCreate(ReadOnlySpan<byte> bytes, out FieldElement field)
		{
			uint n0, n1, n2, n3, n4, n5, n6, n7, n8, n9;
			int magnitude;
			bool normalized;
			n0 = (uint)bytes[31] | ((uint)bytes[30] << 8) | ((uint)bytes[29] << 16) | ((uint)(bytes[28] & 0x3) << 24);
			n1 = (uint)((bytes[28] >> 2) & 0x3f) | ((uint)bytes[27] << 6) | ((uint)bytes[26] << 14) | ((uint)(bytes[25] & 0xf) << 22);
			n2 = (uint)((bytes[25] >> 4) & 0xf) | ((uint)bytes[24] << 4) | ((uint)bytes[23] << 12) | ((uint)(bytes[22] & 0x3f) << 20);
			n3 = (uint)((bytes[22] >> 6) & 0x3) | ((uint)bytes[21] << 2) | ((uint)bytes[20] << 10) | ((uint)bytes[19] << 18);
			n4 = (uint)bytes[18] | ((uint)bytes[17] << 8) | ((uint)bytes[16] << 16) | ((uint)(bytes[15] & 0x3) << 24);
			n5 = (uint)((bytes[15] >> 2) & 0x3f) | ((uint)bytes[14] << 6) | ((uint)bytes[13] << 14) | ((uint)(bytes[12] & 0xf) << 22);
			n6 = (uint)((bytes[12] >> 4) & 0xf) | ((uint)bytes[11] << 4) | ((uint)bytes[10] << 12) | ((uint)(bytes[9] & 0x3f) << 20);
			n7 = (uint)((bytes[9] >> 6) & 0x3) | ((uint)bytes[8] << 2) | ((uint)bytes[7] << 10) | ((uint)bytes[6] << 18);
			n8 = (uint)bytes[5] | ((uint)bytes[4] << 8) | ((uint)bytes[3] << 16) | ((uint)(bytes[2] & 0x3) << 24);
			n9 = (uint)((bytes[2] >> 2) & 0x3f) | ((uint)bytes[1] << 6) | ((uint)bytes[0] << 14);
			if (n9 == 0x3FFFFFUL && (n8 & n7 & n6 & n5 & n4 & n3 & n2) == 0x3FFFFFFUL && (n1 + 0x40UL + ((n0 + 0x3D1UL) >> 26)) > 0x3FFFFFFUL)
			{
				field = default;
				return false;
			}
			magnitude = 1;
			normalized = true;
			field = new FieldElement(n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized);
			return true;
		}

		public FieldElement(uint n0, uint n1, uint n2, uint n3, uint n4, uint n5, uint n6, uint n7, uint n8, uint n9, int magnitude, bool normalized)
		{
			this.n0 = n0;
			this.n1 = n1;
			this.n2 = n2;
			this.n3 = n3;
			this.n4 = n4;
			this.n5 = n5;
			this.n6 = n6;
			this.n7 = n7;
			this.n8 = n8;
			this.n9 = n9;
			this.magnitude = magnitude;
			this.normalized = normalized;
		}

		public readonly void Deconstruct(
			out uint n0,
			out uint n1,
			out uint n2,
			out uint n3,
			out uint n4,
			out uint n5,
			out uint n6,
			out uint n7,
			out uint n8,
			out uint n9,
			out int magnitude,
			out bool normalized
			)
		{
			n0 = this.n0;
			n1 = this.n1;
			n2 = this.n2;
			n3 = this.n3;
			n4 = this.n4;
			n5 = this.n5;
			n6 = this.n6;
			n7 = this.n7;
			n8 = this.n8;
			n9 = this.n9;
			magnitude = this.magnitude;
			normalized = this.normalized;
		}


		public readonly bool IsZero
		{
			[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.AggressiveInlining)]
			get
			{
				VERIFY_CHECK(normalized);
				VERIFY();
				return (n0 | n1 | n2 | n3 | n4 | n5 | n6 | n7 | n8 | n9) == 0;
			}
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly FieldElement Negate(int m)
		{
			ref readonly FieldElement a = ref this;
			VERIFY_CHECK(this.magnitude <= m);
			VERIFY();
			var (n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized) = this;
			n0 = (uint)(0x3FFFC2FUL * 2 * (uint)(m + 1) - a.n0);
			n1 = (uint)(0x3FFFFBFUL * 2 * (uint)(m + 1) - a.n1);
			n2 = (uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - a.n2);
			n3 = (uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - a.n3);
			n4 = (uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - a.n4);
			n5 = (uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - a.n5);
			n6 = (uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - a.n6);
			n7 = (uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - a.n7);
			n8 = (uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - a.n8);
			n9 = (uint)(0x03FFFFFUL * 2 * (uint)(m + 1) - a.n9);
			magnitude = m + 1;
			normalized = false;
			var result = new FieldElement(n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized);
			result.VERIFY();
			return result;
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly FieldElement NormalizeWeak()
		{
			var (n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized) = this;
			uint t0 = n0, t1 = n1, t2 = n2, t3 = n3, t4 = n4,
			t5 = n5, t6 = n6, t7 = n7, t8 = n8, t9 = n9;

			/* Reduce t9 at the start so there will be at most a single carry from the first pass */
			uint x = t9 >> 22; t9 &= 0x03FFFFFU;

			/* The first pass ensures the magnitude is 1, ... */
			t0 += x * 0x3D1U; t1 += (x << 6);
			t1 += (t0 >> 26); t0 &= 0x3FFFFFFU;
			t2 += (t1 >> 26); t1 &= 0x3FFFFFFU;
			t3 += (t2 >> 26); t2 &= 0x3FFFFFFU;
			t4 += (t3 >> 26); t3 &= 0x3FFFFFFU;
			t5 += (t4 >> 26); t4 &= 0x3FFFFFFU;
			t6 += (t5 >> 26); t5 &= 0x3FFFFFFU;
			t7 += (t6 >> 26); t6 &= 0x3FFFFFFU;
			t8 += (t7 >> 26); t7 &= 0x3FFFFFFU;
			t9 += (t8 >> 26); t8 &= 0x3FFFFFFU;

			/* ... except for a possible carry at bit 22 of t9 (i.e. bit 256 of the field element) */
			VERIFY_CHECK(t9 >> 23 == 0);

			n0 = t0; n1 = t1; n2 = t2; n3 = t3; n4 = t4;
			n5 = t5; n6 = t6; n7 = t7; n8 = t8; n9 = t9;
			magnitude = 1;
			var result = new FieldElement(n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized);
			result.VERIFY();
			return result;
		}

		public readonly FieldElement NormalizeVariable()
		{
			var (n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized) = this;
			uint t0 = n0, t1 = n1, t2 = n2, t3 = n3, t4 = n4,
			t5 = n5, t6 = n6, t7 = n7, t8 = n8, t9 = n9;

			/* Reduce t9 at the start so there will be at most a single carry from the first pass */
			uint m;
			uint x = t9 >> 22; t9 &= 0x03FFFFFU;

			/* The first pass ensures the magnitude is 1, ... */
			t0 += x * 0x3D1U; t1 += (x << 6);
			t1 += (t0 >> 26); t0 &= 0x3FFFFFFU;
			t2 += (t1 >> 26); t1 &= 0x3FFFFFFU;
			t3 += (t2 >> 26); t2 &= 0x3FFFFFFU; m = t2;
			t4 += (t3 >> 26); t3 &= 0x3FFFFFFU; m &= t3;
			t5 += (t4 >> 26); t4 &= 0x3FFFFFFU; m &= t4;
			t6 += (t5 >> 26); t5 &= 0x3FFFFFFU; m &= t5;
			t7 += (t6 >> 26); t6 &= 0x3FFFFFFU; m &= t6;
			t8 += (t7 >> 26); t7 &= 0x3FFFFFFU; m &= t7;
			t9 += (t8 >> 26); t8 &= 0x3FFFFFFU; m &= t8;

			/* ... except for a possible carry at bit 22 of t9 (i.e. bit 256 of the field element) */
			VERIFY_CHECK(t9 >> 23 == 0);

			/* At most a single final reduction is needed; check if the value is >= the field characteristic */
			x = (t9 >> 22) | ((t9 == 0x03FFFFFU ? 1U : 0) & (m == 0x3FFFFFFU ? 1U : 0)
				& ((t1 + 0x40U + ((t0 + 0x3D1U) >> 26)) > 0x3FFFFFFU ? 1U : 0));

			if (x != 0)
			{
				t0 += 0x3D1U; t1 += (x << 6);
				t1 += (t0 >> 26); t0 &= 0x3FFFFFFU;
				t2 += (t1 >> 26); t1 &= 0x3FFFFFFU;
				t3 += (t2 >> 26); t2 &= 0x3FFFFFFU;
				t4 += (t3 >> 26); t3 &= 0x3FFFFFFU;
				t5 += (t4 >> 26); t4 &= 0x3FFFFFFU;
				t6 += (t5 >> 26); t5 &= 0x3FFFFFFU;
				t7 += (t6 >> 26); t6 &= 0x3FFFFFFU;
				t8 += (t7 >> 26); t7 &= 0x3FFFFFFU;
				t9 += (t8 >> 26); t8 &= 0x3FFFFFFU;

				/* If t9 didn't carry to bit 22 already, then it should have after any final reduction */
				VERIFY_CHECK(t9 >> 22 == x);

				/* Mask off the possible multiple of 2^256 from the final reduction */
				t9 &= 0x03FFFFFU;
			}

			n0 = t0; n1 = t1; n2 = t2; n3 = t3; n4 = t4;
			n5 = t5; n6 = t6; n7 = t7; n8 = t8; n9 = t9;

			magnitude = 1;
			normalized = true;
			var result = new FieldElement(n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized);
			result.VERIFY();
			return result;
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly FieldElement Normalize()
		{
			var (n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized) = this;

			uint t0 = n0, t1 = n1, t2 = n2, t3 = n3, t4 = n4,
			 t5 = n5, t6 = n6, t7 = n7, t8 = n8, t9 = n9;

			/* Reduce t9 at the start so there will be at most a single carry from the first pass */
			uint m;
			uint x = t9 >> 22; t9 &= 0x03FFFFFU;

			/* The first pass ensures the magnitude is 1, ... */
			t0 += x * 0x3D1U; t1 += (x << 6);
			t1 += (t0 >> 26); t0 &= 0x3FFFFFFU;
			t2 += (t1 >> 26); t1 &= 0x3FFFFFFU;
			t3 += (t2 >> 26); t2 &= 0x3FFFFFFU; m = t2;
			t4 += (t3 >> 26); t3 &= 0x3FFFFFFU; m &= t3;
			t5 += (t4 >> 26); t4 &= 0x3FFFFFFU; m &= t4;
			t6 += (t5 >> 26); t5 &= 0x3FFFFFFU; m &= t5;
			t7 += (t6 >> 26); t6 &= 0x3FFFFFFU; m &= t6;
			t8 += (t7 >> 26); t7 &= 0x3FFFFFFU; m &= t7;
			t9 += (t8 >> 26); t8 &= 0x3FFFFFFU; m &= t8;

			/* ... except for a possible carry at bit 22 of t9 (i.e. bit 256 of the field element) */
			VERIFY_CHECK(t9 >> 23 == 0);

			/* At most a single final reduction is needed; check if the value is >= the field characteristic */
			x = (t9 >> 22) | ((t9 == 0x03FFFFFU ? 1u : 0) & (m == 0x3FFFFFFU ? 1u : 0)
				& ((t1 + 0x40U + ((t0 + 0x3D1U) >> 26)) > 0x3FFFFFFU ? 1u : 0));

			/* Apply the final reduction (for constant-time behaviour, we do it always) */
			t0 += x * 0x3D1U; t1 += (x << 6);
			t1 += (t0 >> 26); t0 &= 0x3FFFFFFU;
			t2 += (t1 >> 26); t1 &= 0x3FFFFFFU;
			t3 += (t2 >> 26); t2 &= 0x3FFFFFFU;
			t4 += (t3 >> 26); t3 &= 0x3FFFFFFU;
			t5 += (t4 >> 26); t4 &= 0x3FFFFFFU;
			t6 += (t5 >> 26); t5 &= 0x3FFFFFFU;
			t7 += (t6 >> 26); t6 &= 0x3FFFFFFU;
			t8 += (t7 >> 26); t7 &= 0x3FFFFFFU;
			t9 += (t8 >> 26); t8 &= 0x3FFFFFFU;

			/* If t9 didn't carry to bit 22 already, then it should have after any final reduction */
			VERIFY_CHECK(t9 >> 22 == x);

			/* Mask off the possible multiple of 2^256 from the final reduction */
			t9 &= 0x03FFFFFU;

			n0 = t0; n1 = t1; n2 = t2; n3 = t3; n4 = t4;
			n5 = t5; n6 = t6; n7 = t7; n8 = t8; n9 = t9;
			magnitude = 1;
			normalized = true;
			var result = new FieldElement(n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, magnitude, normalized);
			result.VERIFY();
			return result;
		}

		public readonly void WriteToSpan(Span<byte> r)
		{
			this.VERIFY();
			VERIFY_CHECK(normalized);
			r[0] = (byte)((n9 >> 14) & 0xff);
			r[1] = (byte)((n9 >> 6) & 0xff);
			r[2] = (byte)(((n9 & 0x3F) << 2) | ((n8 >> 24) & 0x3));
			r[3] = (byte)((n8 >> 16) & 0xff);
			r[4] = (byte)((n8 >> 8) & 0xff);
			r[5] = (byte)(n8 & 0xff);
			r[6] = (byte)((n7 >> 18) & 0xff);
			r[7] = (byte)((n7 >> 10) & 0xff);
			r[8] = (byte)((n7 >> 2) & 0xff);
			r[9] = (byte)(((n7 & 0x3) << 6) | ((n6 >> 20) & 0x3f));
			r[10] = (byte)((n6 >> 12) & 0xff);
			r[11] = (byte)((n6 >> 4) & 0xff);
			r[12] = (byte)(((n6 & 0xf) << 4) | ((n5 >> 22) & 0xf));
			r[13] = (byte)((n5 >> 14) & 0xff);
			r[14] = (byte)((n5 >> 6) & 0xff);
			r[15] = (byte)(((n5 & 0x3f) << 2) | ((n4 >> 24) & 0x3));
			r[16] = (byte)((n4 >> 16) & 0xff);
			r[17] = (byte)((n4 >> 8) & 0xff);
			r[18] = (byte)(n4 & 0xff);
			r[19] = (byte)((n3 >> 18) & 0xff);
			r[20] = (byte)((n3 >> 10) & 0xff);
			r[21] = (byte)((n3 >> 2) & 0xff);
			r[22] = (byte)(((n3 & 0x3) << 6) | ((n2 >> 20) & 0x3f));
			r[23] = (byte)((n2 >> 12) & 0xff);
			r[24] = (byte)((n2 >> 4) & 0xff);
			r[25] = (byte)(((n2 & 0xf) << 4) | ((n1 >> 22) & 0xf));
			r[26] = (byte)((n1 >> 14) & 0xff);
			r[27] = (byte)((n1 >> 6) & 0xff);
			r[28] = (byte)(((n1 & 0x3f) << 2) | ((n0 >> 24) & 0x3));
			r[29] = (byte)((n0 >> 16) & 0xff);
			r[30] = (byte)((n0 >> 8) & 0xff);
			r[31] = (byte)(n0 & 0xff);
		}

		private readonly void VERIFY()
		{
			int m = normalized ? 1 : 2 * magnitude, r = 1;
			r &= (n0 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n1 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n2 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n3 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n4 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n5 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n6 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n7 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n8 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n9 <= 0x03FFFFFUL * (uint)m) ? 1 : 0;
			r &= (magnitude >= 0 ? 1 : 0);
			r &= (magnitude <= 32 ? 1 : 0);
			if (normalized)
			{
				r &= (magnitude <= 1 ? 1 : 0);
				if (r != 0 && (n9 == 0x03FFFFFUL))
				{
					uint mid = n8 & n7 & n6 & n5 & n4 & n3 & n2;
					if (mid == 0x3FFFFFFUL)
					{
						r &= ((n1 + 0x40UL + ((n0 + 0x3D1UL) >> 26)) <= 0x3FFFFFFUL) ? 1 : 0;
					}
				}
			}
			VERIFY_CHECK(r == 1);
		}

		private static void VERIFY_CHECK(bool value)
		{
			if (!value)
				throw new InvalidOperationException("VERIFY_CHECK failed (bug in C# secp256k1)");
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly bool Equals(FieldElement b)
		{
			ref readonly FieldElement a = ref this;
			var na = a.Negate(1);
			na += b;
			return na.NormalizesToZero();
		}
		public readonly bool EqualsVariable(in FieldElement b)
		{
			ref readonly FieldElement a = ref this;
			var na = a.Negate(1);
			na += b;
			return na.NormalizesToZero();
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly bool NormalizesToZero()
		{
			uint t0 = n0, t1 = n1, t2 = n2, t3 = n3, t4 = n4,
		 t5 = n5, t6 = n6, t7 = n7, t8 = n8, t9 = n9;

			/* z0 tracks a possible raw value of 0, z1 tracks a possible raw value of P */
			uint z0, z1;

			/* Reduce t9 at the start so there will be at most a single carry from the first pass */
			uint x = t9 >> 22; t9 &= 0x03FFFFFU;

			/* The first pass ensures the magnitude is 1, ... */
			t0 += x * 0x3D1U; t1 += (x << 6);
			t1 += (t0 >> 26); t0 &= 0x3FFFFFFU; z0 = t0; z1 = t0 ^ 0x3D0U;
			t2 += (t1 >> 26); t1 &= 0x3FFFFFFU; z0 |= t1; z1 &= t1 ^ 0x40U;
			t3 += (t2 >> 26); t2 &= 0x3FFFFFFU; z0 |= t2; z1 &= t2;
			t4 += (t3 >> 26); t3 &= 0x3FFFFFFU; z0 |= t3; z1 &= t3;
			t5 += (t4 >> 26); t4 &= 0x3FFFFFFU; z0 |= t4; z1 &= t4;
			t6 += (t5 >> 26); t5 &= 0x3FFFFFFU; z0 |= t5; z1 &= t5;
			t7 += (t6 >> 26); t6 &= 0x3FFFFFFU; z0 |= t6; z1 &= t6;
			t8 += (t7 >> 26); t7 &= 0x3FFFFFFU; z0 |= t7; z1 &= t7;
			t9 += (t8 >> 26); t8 &= 0x3FFFFFFU; z0 |= t8; z1 &= t8;
			z0 |= t9; z1 &= t9 ^ 0x3C00000U;

			/* ... except for a possible carry at bit 22 of t9 (i.e. bit 256 of the field element) */
			VERIFY_CHECK(t9 >> 23 == 0);

			return ((z0 == 0 ? 1 : 0) | (z1 == 0x3FFFFFFU ? 1 : 0)) != 0;
		}

		public static bool operator ==(in FieldElement a, in FieldElement b)
		{
			return a.Equals(b);
		}
		public static bool operator !=(in FieldElement a, in FieldElement b)
		{
			return !a.Equals(b);
		}
		public static FieldElement operator *(in FieldElement a, in FieldElement b)
		{
			return a.Multiply(b);
		}
		public static FieldElement operator *(in FieldElement a, in uint b)
		{
			return a.Multiply(b);
		}
		public static FieldElement operator +(in FieldElement a, in FieldElement b)
		{
			return a.Add(b);
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + n0.GetHashCode();
				hash = hash * 23 + n1.GetHashCode();
				hash = hash * 23 + n2.GetHashCode();
				hash = hash * 23 + n3.GetHashCode();
				hash = hash * 23 + n4.GetHashCode();
				hash = hash * 23 + n5.GetHashCode();
				hash = hash * 23 + n6.GetHashCode();
				hash = hash * 23 + n7.GetHashCode();
				hash = hash * 23 + n8.GetHashCode();
				hash = hash * 23 + n9.GetHashCode();
				return hash;
			}
		}

		public readonly override bool Equals(object obj)
		{
			if (obj is FieldElement other)
			{
				return this.Equals(other);
			}
			return false;
		}
	}
}
