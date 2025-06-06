﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Tests;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    public sealed class Vector512Tests
    {
        /// <summary>Verifies that two <see cref="Vector512{Single}" /> values are equal, within the <paramref name="variance" />.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="variance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        internal static void AssertEqual(Vector512<float> expected, Vector512<float> actual, Vector512<float> variance)
        {
            Vector256Tests.AssertEqual(expected.GetLower(), actual.GetLower(), variance.GetLower());
            Vector256Tests.AssertEqual(expected.GetUpper(), actual.GetUpper(), variance.GetUpper());
        }

        /// <summary>Verifies that two <see cref="Vector512{Double}" /> values are equal, within the <paramref name="variance" />.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="variance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        internal static void AssertEqual(Vector512<double> expected, Vector512<double> actual, Vector512<double> variance)
        {
            Vector256Tests.AssertEqual(expected.GetLower(), actual.GetLower(), variance.GetLower());
            Vector256Tests.AssertEqual(expected.GetUpper(), actual.GetUpper(), variance.GetUpper());
        }

        [Fact]
        public unsafe void Vector512IsHardwareAcceleratedTest()
        {
            MethodInfo methodInfo = typeof(Vector512).GetMethod("get_IsHardwareAccelerated");
            Assert.Equal(Vector512.IsHardwareAccelerated, methodInfo.Invoke(null, null));
        }

        [Fact]
        public unsafe void Vector512ByteExtractMostSignificantBitsTest()
        {
            Vector512<byte> vector = Vector512.Create(
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80
            );

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512DoubleExtractMostSignificantBitsTest()
        {
            Vector512<double> vector = Vector512.Create(
                +1.0,
                -0.0,
                +1.0,
                -0.0,
                +1.0,
                -0.0,
                +1.0,
                -0.0
            );

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512Int16ExtractMostSignificantBitsTest()
        {
            Vector512<short> vector = Vector512.Create(
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000
            ).AsInt16();

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010_10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512Int32ExtractMostSignificantBitsTest()
        {
            Vector512<int> vector = Vector512.Create(
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U
            ).AsInt32();

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512Int64ExtractMostSignificantBitsTest()
        {
            Vector512<long> vector = Vector512.Create(
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL
            ).AsInt64();

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1010_1010UL, result);
        }

        [Fact]
        public unsafe void Vector512NIntExtractMostSignificantBitsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector512<nint> vector = Vector512.Create(
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL
                ).AsNInt();

                ulong result = Vector512.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10101010UL, result);
            }
            else
            {
                Vector512<nint> vector = Vector512.Create(
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U
                ).AsNInt();

                ulong result = Vector512.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10101010_10101010UL, result);
            }
        }

        [Fact]
        public unsafe void Vector512NUIntExtractMostSignificantBitsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector512<nuint> vector = Vector512.Create(
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL
                ).AsNUInt();

                ulong result = Vector512.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10101010UL, result);
            }
            else
            {
                Vector512<nuint> vector = Vector512.Create(
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U
                ).AsNUInt();

                ulong result = Vector512.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10101010_10101010UL, result);
            }
        }

        [Fact]
        public unsafe void Vector512SByteExtractMostSignificantBitsTest()
        {
            Vector512<sbyte> vector = Vector512.Create(
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80
            ).AsSByte();

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512SingleExtractMostSignificantBitsTest()
        {
            Vector512<float> vector = Vector512.Create(
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f
            );

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512UInt16ExtractMostSignificantBitsTest()
        {
            Vector512<ushort> vector = Vector512.Create(
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000
            );

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010_10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512UInt32ExtractMostSignificantBitsTest()
        {
            Vector512<uint> vector = Vector512.Create(
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U
            );

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512UInt64ExtractMostSignificantBitsTest()
        {
            Vector512<ulong> vector = Vector512.Create(
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL
            );

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512ByteLoadTest()
        {
            byte* value = stackalloc byte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (byte)(index);
            }

            Vector512<byte> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512DoubleLoadTest()
        {
            double* value = stackalloc double[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512<double> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int16LoadTest()
        {
            short* value = stackalloc short[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (short)(index);
            }

            Vector512<short> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int32LoadTest()
        {
            int* value = stackalloc int[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512<int> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int64LoadTest()
        {
            long* value = stackalloc long[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512<long> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512NIntLoadTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512<nint> vector = Vector512.Load(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512<nint> vector = Vector512.Load(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector512NUIntLoadTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512<nuint> vector = Vector512.Load(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512<nuint> vector = Vector512.Load(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector512SByteLoadTest()
        {
            sbyte* value = stackalloc sbyte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (sbyte)(index);
            }

            Vector512<sbyte> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512SingleLoadTest()
        {
            float* value = stackalloc float[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512<float> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt16LoadTest()
        {
            ushort* value = stackalloc ushort[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (ushort)(index);
            }

            Vector512<ushort> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt32LoadTest()
        {
            uint* value = stackalloc uint[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = (uint)(index);
            }

            Vector512<uint> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt64LoadTest()
        {
            ulong* value = stackalloc ulong[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = (ulong)(index);
            }

            Vector512<ulong> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512ByteLoadAlignedTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (byte)(index);
                }

                Vector512<byte> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<byte>.Count; index++)
                {
                    Assert.Equal((byte)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleLoadAlignedTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512<double> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<double>.Count; index++)
                {
                    Assert.Equal((double)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int16LoadAlignedTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (short)(index);
                }

                Vector512<short> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<short>.Count; index++)
                {
                    Assert.Equal((short)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int32LoadAlignedTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512<int> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<int>.Count; index++)
                {
                    Assert.Equal((int)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int64LoadAlignedTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512<long> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<long>.Count; index++)
                {
                    Assert.Equal((long)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NIntLoadAlignedTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = index;
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = index;
                    }
                }

                Vector512<nint> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NUIntLoadAlignedTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }

                Vector512<nuint> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SByteLoadAlignedTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (sbyte)(index);
                }

                Vector512<sbyte> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<sbyte>.Count; index++)
                {
                    Assert.Equal((sbyte)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SingleLoadAlignedTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512<float> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<float>.Count; index++)
                {
                    Assert.Equal((float)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16LoadAlignedTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (ushort)(index);
                }

                Vector512<ushort> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<ushort>.Count; index++)
                {
                    Assert.Equal((ushort)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32LoadAlignedTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (uint)(index);
                }

                Vector512<uint> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<uint>.Count; index++)
                {
                    Assert.Equal((uint)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64LoadAlignedTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (ulong)(index);
                }

                Vector512<ulong> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<ulong>.Count; index++)
                {
                    Assert.Equal((ulong)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512ByteLoadAlignedNonTemporalTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (byte)(index);
                }

                Vector512<byte> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<byte>.Count; index++)
                {
                    Assert.Equal((byte)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleLoadAlignedNonTemporalTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512<double> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<double>.Count; index++)
                {
                    Assert.Equal((double)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int16LoadAlignedNonTemporalTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (short)(index);
                }

                Vector512<short> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<short>.Count; index++)
                {
                    Assert.Equal((short)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int32LoadAlignedNonTemporalTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512<int> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<int>.Count; index++)
                {
                    Assert.Equal((int)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int64LoadAlignedNonTemporalTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512<long> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<long>.Count; index++)
                {
                    Assert.Equal((long)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NIntLoadAlignedNonTemporalTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = index;
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = index;
                    }
                }

                Vector512<nint> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NUIntLoadAlignedNonTemporalTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }

                Vector512<nuint> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SByteLoadAlignedNonTemporalTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (sbyte)(index);
                }

                Vector512<sbyte> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<sbyte>.Count; index++)
                {
                    Assert.Equal((sbyte)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SingleLoadAlignedNonTemporalTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512<float> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<float>.Count; index++)
                {
                    Assert.Equal((float)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16LoadAlignedNonTemporalTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (ushort)(index);
                }

                Vector512<ushort> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<ushort>.Count; index++)
                {
                    Assert.Equal((ushort)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32LoadAlignedNonTemporalTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (uint)(index);
                }

                Vector512<uint> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<uint>.Count; index++)
                {
                    Assert.Equal((uint)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64LoadAlignedNonTemporalTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (ulong)(index);
                }

                Vector512<ulong> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<ulong>.Count; index++)
                {
                    Assert.Equal((ulong)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512ByteLoadUnsafeTest()
        {
            byte* value = stackalloc byte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (byte)(index);
            }

            Vector512<byte> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512DoubleLoadUnsafeTest()
        {
            double* value = stackalloc double[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512<double> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int16LoadUnsafeTest()
        {
            short* value = stackalloc short[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (short)(index);
            }

            Vector512<short> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int32LoadUnsafeTest()
        {
            int* value = stackalloc int[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512<int> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int64LoadUnsafeTest()
        {
            long* value = stackalloc long[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512<long> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512NIntLoadUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512<nint> vector = Vector512.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512<nint> vector = Vector512.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector512NUIntLoadUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512<nuint> vector = Vector512.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512<nuint> vector = Vector512.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector512SByteLoadUnsafeTest()
        {
            sbyte* value = stackalloc sbyte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (sbyte)(index);
            }

            Vector512<sbyte> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512SingleLoadUnsafeTest()
        {
            float* value = stackalloc float[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512<float> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt16LoadUnsafeTest()
        {
            ushort* value = stackalloc ushort[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (ushort)(index);
            }

            Vector512<ushort> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt32LoadUnsafeTest()
        {
            uint* value = stackalloc uint[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = (uint)(index);
            }

            Vector512<uint> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt64LoadUnsafeTest()
        {
            ulong* value = stackalloc ulong[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = (ulong)(index);
            }

            Vector512<ulong> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512ByteLoadUnsafeIndexTest()
        {
            byte* value = stackalloc byte[64 + 1];

            for (int index = 0; index < 64 + 1; index++)
            {
                value[index] = (byte)(index);
            }

            Vector512<byte> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512DoubleLoadUnsafeIndexTest()
        {
            double* value = stackalloc double[8 + 1];

            for (int index = 0; index < 8 + 1; index++)
            {
                value[index] = index;
            }

            Vector512<double> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int16LoadUnsafeIndexTest()
        {
            short* value = stackalloc short[32 + 1];

            for (int index = 0; index < 32 + 1; index++)
            {
                value[index] = (short)(index);
            }

            Vector512<short> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int32LoadUnsafeIndexTest()
        {
            int* value = stackalloc int[16 + 1];

            for (int index = 0; index < 16 + 1; index++)
            {
                value[index] = index;
            }

            Vector512<int> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int64LoadUnsafeIndexTest()
        {
            long* value = stackalloc long[8 + 1];

            for (int index = 0; index < 8 + 1; index++)
            {
                value[index] = index;
            }

            Vector512<long> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512NIntLoadUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[8 + 1];

                for (int index = 0; index < 8 + 1; index++)
                {
                    value[index] = index;
                }

                Vector512<nint> vector = Vector512.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)(index + 1), vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[16 + 1];

                for (int index = 0; index < 16 + 1; index++)
                {
                    value[index] = index;
                }

                Vector512<nint> vector = Vector512.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)(index + 1), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector512NUIntLoadUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[8 + 1];

                for (int index = 0; index < 8 + 1; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512<nuint> vector = Vector512.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)(index + 1), vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[16 + 1];

                for (int index = 0; index < 16 + 1; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512<nuint> vector = Vector512.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)(index + 1), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector512SByteLoadUnsafeIndexTest()
        {
            sbyte* value = stackalloc sbyte[64 + 1];

            for (int index = 0; index < 64 + 1; index++)
            {
                value[index] = (sbyte)(index);
            }

            Vector512<sbyte> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512SingleLoadUnsafeIndexTest()
        {
            float* value = stackalloc float[16 + 1];

            for (int index = 0; index < 16 + 1; index++)
            {
                value[index] = index;
            }

            Vector512<float> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt16LoadUnsafeIndexTest()
        {
            ushort* value = stackalloc ushort[32 + 1];

            for (int index = 0; index < 32 + 1; index++)
            {
                value[index] = (ushort)(index);
            }

            Vector512<ushort> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt32LoadUnsafeIndexTest()
        {
            uint* value = stackalloc uint[16 + 1];

            for (int index = 0; index < 16 + 1; index++)
            {
                value[index] = (uint)(index);
            }

            Vector512<uint> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt64LoadUnsafeIndexTest()
        {
            ulong* value = stackalloc ulong[8 + 1];

            for (int index = 0; index < 8 + 1; index++)
            {
                value[index] = (ulong)(index);
            }

            Vector512<ulong> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShiftLeftTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShiftLeftTest()
        {
            Vector512<short> vector = Vector512.Create((short)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShiftLeftTest()
        {
            Vector512<int> vector = Vector512.Create((int)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShiftLeftTest()
        {
            Vector512<long> vector = Vector512.Create((long)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512NIntShiftLeftTest()
        {
            Vector512<nint> vector = Vector512.Create((nint)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<nint>.Count; index++)
            {
                Assert.Equal((nint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512NUIntShiftLeftTest()
        {
            Vector512<nuint> vector = Vector512.Create((nuint)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<nuint>.Count; index++)
            {
                Assert.Equal((nuint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShiftLeftTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShiftLeftTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShiftLeftTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShiftLeftTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShiftRightArithmeticTest()
        {
            Vector512<short> vector = Vector512.Create(unchecked((short)0x8000));
            vector = Vector512.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal(unchecked((short)0xF800), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShiftRightArithmeticTest()
        {
            Vector512<int> vector = Vector512.Create(unchecked((int)0x80000000));
            vector = Vector512.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal(unchecked((int)0xF8000000), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShiftRightArithmeticTest()
        {
            Vector512<long> vector = Vector512.Create(unchecked((long)0x8000000000000000));
            vector = Vector512.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal(unchecked((long)0xF800000000000000), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512NIntShiftRightArithmeticTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector512<nint> vector = Vector512.Create(unchecked((nint)0x8000000000000000));
                vector = Vector512.ShiftRightArithmetic(vector, 4);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0xF800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector512<nint> vector = Vector512.Create(unchecked((nint)0x80000000));
                vector = Vector512.ShiftRightArithmetic(vector, 4);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0xF8000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector512SByteShiftRightArithmeticTest()
        {
            Vector512<sbyte> vector = Vector512.Create(unchecked((sbyte)0x80));
            vector = Vector512.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal(unchecked((sbyte)0xF8), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShiftRightLogicalTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)0x80);
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)0x08, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShiftRightLogicalTest()
        {
            Vector512<short> vector = Vector512.Create(unchecked((short)0x8000));
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)0x0800, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShiftRightLogicalTest()
        {
            Vector512<int> vector = Vector512.Create(unchecked((int)0x80000000));
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)0x08000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShiftRightLogicalTest()
        {
            Vector512<long> vector = Vector512.Create(unchecked((long)0x8000000000000000));
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)0x0800000000000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512NIntShiftRightLogicalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector512<nint> vector = Vector512.Create(unchecked((nint)0x8000000000000000));
                vector = Vector512.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0x0800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector512<nint> vector = Vector512.Create(unchecked((nint)0x80000000));
                vector = Vector512.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0x08000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector512NUIntShiftRightLogicalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector512<nuint> vector = Vector512.Create(unchecked((nuint)0x8000000000000000));
                vector = Vector512.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal(unchecked((nuint)0x0800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector512<nuint> vector = Vector512.Create(unchecked((nuint)0x80000000));
                vector = Vector512.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal(unchecked((nuint)0x08000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector512SByteShiftRightLogicalTest()
        {
            Vector512<sbyte> vector = Vector512.Create(unchecked((sbyte)0x80));
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x08, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShiftRightLogicalTest()
        {
            Vector512<ushort> vector = Vector512.Create(unchecked((ushort)0x8000));
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x0800, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShiftRightLogicalTest()
        {
            Vector512<uint> vector = Vector512.Create(0x80000000);
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)0x08000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShiftRightLogicalTest()
        {
            Vector512<ulong> vector = Vector512.Create(0x8000000000000000);
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x0800000000000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<byte> result = Vector512.Shuffle(vector, Vector512.Create((byte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputTest()
        {
            Vector512<double> vector = Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<double> result = Vector512.Shuffle(vector, Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputTest()
        {
            Vector512<short> vector = Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<short> result = Vector512.Shuffle(vector, Vector512.Create((short)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputTest()
        {
            Vector512<int> vector = Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> result = Vector512.Shuffle(vector, Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputTest()
        {
            Vector512<long> vector = Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> result = Vector512.Shuffle(vector, Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<sbyte> result = Vector512.Shuffle(vector, Vector512.Create((sbyte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputTest()
        {
            Vector512<float> vector = Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<float> result = Vector512.Shuffle(vector, Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<ushort> result = Vector512.Shuffle(vector, Vector512.Create((ushort)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<uint> result = Vector512.Shuffle(vector, Vector512.Create((uint)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<ulong> result = Vector512.Shuffle(vector, Vector512.Create((ulong)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputWithDirectVectorTest()
        {
            Vector512<byte> result = Vector512.Shuffle(
                Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((byte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputWithDirectVectorTest()
        {
            Vector512<double> result = Vector512.Shuffle(
                Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputWithDirectVectorTest()
        {
            Vector512<short> result = Vector512.Shuffle(
                Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((short)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputWithDirectVectorTest()
        {
            Vector512<int> result = Vector512.Shuffle(
                Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputWithDirectVectorTest()
        {
            Vector512<long> result = Vector512.Shuffle(
                Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputWithDirectVectorTest()
        {
            Vector512<sbyte> result = Vector512.Shuffle(
                Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((sbyte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputWithDirectVectorTest()
        {
            Vector512<float> result = Vector512.Shuffle(
                Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputWithDirectVectorTest()
        {
            Vector512<ushort> result = Vector512.Shuffle(
                Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((ushort)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputWithDirectVectorTest()
        {
            Vector512<uint> result = Vector512.Shuffle(
                Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((uint)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputWithDirectVectorTest()
        {
            Vector512<ulong> result = Vector512.Shuffle(
                Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((ulong)7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<byte> result = Vector512.Shuffle(
                Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48)
            );

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector128<byte>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<byte>.Count; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector256<byte>.Count - (index - Vector128<byte>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<byte>.Count; index < Vector512<byte>.Count - Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - Vector128<byte>.Count - (index - Vector256<byte>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<byte>.Count + Vector128<byte>.Count; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - (index - Vector256<byte>.Count - Vector128<byte>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<double> result = Vector512.Shuffle(
                Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)1, 0, 3, 2, 5, 4, 7, 6)
            );

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)(Vector128<double>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<double>.Count; index < Vector256<double>.Count; index++)
            {
                Assert.Equal((double)(Vector256<double>.Count - (index - Vector128<double>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<double>.Count; index < Vector512<double>.Count - Vector128<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - Vector128<double>.Count - (index - Vector256<double>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<double>.Count + Vector128<double>.Count; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - (index - Vector256<double>.Count - Vector128<double>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<short> result = Vector512.Shuffle(
                Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((short)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24)
            );

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)(Vector128<short>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<short>.Count; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)(Vector256<short>.Count - (index - Vector128<short>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<short>.Count; index < Vector512<short>.Count - Vector128<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - Vector128<short>.Count - (index - Vector256<short>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<short>.Count + Vector128<short>.Count; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - (index - Vector256<short>.Count - Vector128<short>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<int> result = Vector512.Shuffle(
                Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)
            );

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)(Vector128<int>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<int>.Count; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)(Vector256<int>.Count - (index - Vector128<int>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<int>.Count; index < Vector512<int>.Count - Vector128<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - Vector128<int>.Count - (index - Vector256<int>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<int>.Count + Vector128<int>.Count; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - (index - Vector256<int>.Count - Vector128<int>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<long> result = Vector512.Shuffle(
                Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)1, 0, 3, 2, 5, 4, 7, 6)
            );

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)(Vector128<long>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<long>.Count; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)(Vector256<long>.Count - (index - Vector128<long>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<long>.Count; index < Vector512<long>.Count - Vector128<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - Vector128<long>.Count - (index - Vector256<long>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<long>.Count + Vector128<long>.Count; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - (index - Vector256<long>.Count - Vector128<long>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<sbyte> result = Vector512.Shuffle(
                Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((sbyte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48)
            );

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector128<sbyte>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<sbyte>.Count; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector256<sbyte>.Count - (index - Vector128<sbyte>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<sbyte>.Count; index < Vector512<sbyte>.Count - Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - Vector128<sbyte>.Count - (index - Vector256<sbyte>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<sbyte>.Count + Vector128<sbyte>.Count; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - (index - Vector256<sbyte>.Count - Vector128<sbyte>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<float> result = Vector512.Shuffle(
                Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)
            );

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)(Vector128<float>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<float>.Count; index < Vector256<float>.Count; index++)
            {
                Assert.Equal((float)(Vector256<float>.Count - (index - Vector128<float>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<float>.Count; index < Vector512<float>.Count - Vector128<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - Vector128<float>.Count - (index - Vector256<float>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<float>.Count + Vector128<float>.Count; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - (index - Vector256<float>.Count - Vector128<float>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<ushort> result = Vector512.Shuffle(
                Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((ushort)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24)
            );

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector128<ushort>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<ushort>.Count; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector256<ushort>.Count - (index - Vector128<ushort>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<ushort>.Count; index < Vector512<ushort>.Count - Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - Vector128<ushort>.Count - (index - Vector256<ushort>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<ushort>.Count + Vector128<ushort>.Count; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - (index - Vector256<ushort>.Count - Vector128<ushort>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<uint> result = Vector512.Shuffle(
                Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((uint)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)
            );

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector128<uint>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<uint>.Count; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector256<uint>.Count - (index - Vector128<uint>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<uint>.Count; index < Vector512<uint>.Count - Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - Vector128<uint>.Count - (index - Vector256<uint>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<uint>.Count + Vector128<uint>.Count; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - (index - Vector256<uint>.Count - Vector128<uint>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<ulong> result = Vector512.Shuffle(
                Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((ulong)1, 0, 3, 2, 5, 4, 7, 6)
            );

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector128<ulong>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<ulong>.Count; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector256<ulong>.Count - (index - Vector128<ulong>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<ulong>.Count; index < Vector512<ulong>.Count - Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - Vector128<ulong>.Count - (index - Vector256<ulong>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<ulong>.Count + Vector128<ulong>.Count; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - (index - Vector256<ulong>.Count - Vector128<ulong>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<byte> result = Vector512.Shuffle(
                Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((byte)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32)
            );

            for (int index = 0; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector256<byte>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<byte>.Count; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - (index - Vector256<byte>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<double> result = Vector512.Shuffle(
                Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)3, 2, 1, 0, 7, 6, 5, 4)
            );

            for (int index = 0; index < Vector256<double>.Count; index++)
            {
                Assert.Equal((double)(Vector256<double>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<double>.Count; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - (index - Vector256<double>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<short> result = Vector512.Shuffle(
                Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((short)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16)
            );

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)(Vector256<short>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<short>.Count; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - (index - Vector256<short>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<int> result = Vector512.Shuffle(
                Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)
            );

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)(Vector256<int>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<int>.Count; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - (index - Vector256<int>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<long> result = Vector512.Shuffle(
                Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)3, 2, 1, 0, 7, 6, 5, 4)
            );

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)(Vector256<long>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<long>.Count; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - (index - Vector256<long>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<sbyte> result = Vector512.Shuffle(
                Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((sbyte)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32)
            );

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector256<sbyte>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<sbyte>.Count; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - (index - Vector256<sbyte>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<float> result = Vector512.Shuffle(
                Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)
            );

            for (int index = 0; index < Vector256<float>.Count; index++)
            {
                Assert.Equal((float)(Vector256<float>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<float>.Count; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - (index - Vector256<float>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<ushort> result = Vector512.Shuffle(
                Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((ushort)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16)
            );

            for (int index = 0; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector256<ushort>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<ushort>.Count; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - (index - Vector256<ushort>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<uint> result = Vector512.Shuffle(
                Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((uint)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)
            );

            for (int index = 0; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector256<uint>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<uint>.Count; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - (index - Vector256<uint>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<ulong> result = Vector512.Shuffle(
                Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((ulong)3, 2, 1, 0, 7, 6, 5, 4)
            );

            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector256<ulong>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<ulong>.Count; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - (index - Vector256<ulong>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<byte> indices = Vector512.Create((byte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<byte> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<double> vector = Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> indices = Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<double> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<short> vector = Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<short> indices = Vector512.Create((short)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<short> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<int> vector = Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> indices = Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<int> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<long> vector = Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> indices = Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<long> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<sbyte> indices = Vector512.Create((sbyte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<sbyte> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<float> vector = Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> indices = Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<float> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<ushort> indices = Vector512.Create((ushort)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<ushort> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<uint> indices = Vector512.Create((uint)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<uint> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<ulong> indices = Vector512.Create((ulong)7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<ulong> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<byte> result = Vector512.Shuffle(vector, Vector512<byte>.AllBitsSet);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<double> vector = Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<double> result = Vector512.Shuffle(vector, Vector512<long>.AllBitsSet);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<short> vector = Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<short> result = Vector512.Shuffle(vector, Vector512<short>.AllBitsSet);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<int> vector = Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> result = Vector512.Shuffle(vector, Vector512<int>.AllBitsSet);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<long> vector = Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> result = Vector512.Shuffle(vector, Vector512<long>.AllBitsSet);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<sbyte> result = Vector512.Shuffle(vector, Vector512<sbyte>.AllBitsSet);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<float> vector = Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<float> result = Vector512.Shuffle(vector, Vector512<int>.AllBitsSet);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<ushort> result = Vector512.Shuffle(vector, Vector512<ushort>.AllBitsSet);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<uint> result = Vector512.Shuffle(vector, Vector512<uint>.AllBitsSet);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<ulong> result = Vector512.Shuffle(vector, Vector512<ulong>.AllBitsSet);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<byte> result = Vector512.Shuffle(vector, Vector512<byte>.Zero);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<double> vector = Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<double> result = Vector512.Shuffle(vector, Vector512<long>.Zero);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<short> vector = Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<short> result = Vector512.Shuffle(vector, Vector512<short>.Zero);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<int> vector = Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> result = Vector512.Shuffle(vector, Vector512<int>.Zero);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<long> vector = Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> result = Vector512.Shuffle(vector, Vector512<long>.Zero);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<sbyte> result = Vector512.Shuffle(vector, Vector512<sbyte>.Zero);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<float> vector = Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<float> result = Vector512.Shuffle(vector, Vector512<int>.Zero);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<ushort> result = Vector512.Shuffle(vector, Vector512<ushort>.Zero);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<uint> result = Vector512.Shuffle(vector, Vector512<uint>.Zero);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<ulong> result = Vector512.Shuffle(vector, Vector512<ulong>.Zero);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleNativeOneInputTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<byte> result = Vector512.ShuffleNative(vector, Vector512.Create((byte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleNativeOneInputTest()
        {
            Vector512<double> vector = Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<double> result = Vector512.ShuffleNative(vector, Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleNativeOneInputTest()
        {
            Vector512<short> vector = Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<short> result = Vector512.ShuffleNative(vector, Vector512.Create((short)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleNativeOneInputTest()
        {
            Vector512<int> vector = Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> result = Vector512.ShuffleNative(vector, Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleNativeOneInputTest()
        {
            Vector512<long> vector = Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> result = Vector512.ShuffleNative(vector, Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleNativeOneInputTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<sbyte> result = Vector512.ShuffleNative(vector, Vector512.Create((sbyte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleNativeOneInputTest()
        {
            Vector512<float> vector = Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<float> result = Vector512.ShuffleNative(vector, Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleNativeOneInputTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<ushort> result = Vector512.ShuffleNative(vector, Vector512.Create((ushort)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleNativeOneInputTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<uint> result = Vector512.ShuffleNative(vector, Vector512.Create((uint)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleNativeOneInputTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<ulong> result = Vector512.ShuffleNative(vector, Vector512.Create((ulong)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleNativeOneInputWithDirectVectorTest()
        {
            Vector512<byte> result = Vector512.ShuffleNative(
                Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((byte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleNativeOneInputWithDirectVectorTest()
        {
            Vector512<double> result = Vector512.ShuffleNative(
                Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleNativeOneInputWithDirectVectorTest()
        {
            Vector512<short> result = Vector512.ShuffleNative(
                Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((short)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleNativeOneInputWithDirectVectorTest()
        {
            Vector512<int> result = Vector512.ShuffleNative(
                Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleNativeOneInputWithDirectVectorTest()
        {
            Vector512<long> result = Vector512.ShuffleNative(
                Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleNativeOneInputWithDirectVectorTest()
        {
            Vector512<sbyte> result = Vector512.ShuffleNative(
                Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((sbyte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleNativeOneInputWithDirectVectorTest()
        {
            Vector512<float> result = Vector512.ShuffleNative(
                Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleNativeOneInputWithDirectVectorTest()
        {
            Vector512<ushort> result = Vector512.ShuffleNative(
                Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((ushort)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleNativeOneInputWithDirectVectorTest()
        {
            Vector512<uint> result = Vector512.ShuffleNative(
                Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((uint)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleNativeOneInputWithDirectVectorTest()
        {
            Vector512<ulong> result = Vector512.ShuffleNative(
                Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((ulong)7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleNativeOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<byte> result = Vector512.ShuffleNative(
                Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48)
            );

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector128<byte>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<byte>.Count; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector256<byte>.Count - (index - Vector128<byte>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<byte>.Count; index < Vector512<byte>.Count - Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - Vector128<byte>.Count - (index - Vector256<byte>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<byte>.Count + Vector128<byte>.Count; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - (index - Vector256<byte>.Count - Vector128<byte>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleNativeOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<double> result = Vector512.ShuffleNative(
                Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)1, 0, 3, 2, 5, 4, 7, 6)
            );

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)(Vector128<double>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<double>.Count; index < Vector256<double>.Count; index++)
            {
                Assert.Equal((double)(Vector256<double>.Count - (index - Vector128<double>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<double>.Count; index < Vector512<double>.Count - Vector128<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - Vector128<double>.Count - (index - Vector256<double>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<double>.Count + Vector128<double>.Count; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - (index - Vector256<double>.Count - Vector128<double>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleNativeOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<short> result = Vector512.ShuffleNative(
                Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((short)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24)
            );

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)(Vector128<short>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<short>.Count; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)(Vector256<short>.Count - (index - Vector128<short>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<short>.Count; index < Vector512<short>.Count - Vector128<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - Vector128<short>.Count - (index - Vector256<short>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<short>.Count + Vector128<short>.Count; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - (index - Vector256<short>.Count - Vector128<short>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleNativeOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<int> result = Vector512.ShuffleNative(
                Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)
            );

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)(Vector128<int>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<int>.Count; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)(Vector256<int>.Count - (index - Vector128<int>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<int>.Count; index < Vector512<int>.Count - Vector128<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - Vector128<int>.Count - (index - Vector256<int>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<int>.Count + Vector128<int>.Count; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - (index - Vector256<int>.Count - Vector128<int>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleNativeOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<long> result = Vector512.ShuffleNative(
                Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)1, 0, 3, 2, 5, 4, 7, 6)
            );

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)(Vector128<long>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<long>.Count; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)(Vector256<long>.Count - (index - Vector128<long>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<long>.Count; index < Vector512<long>.Count - Vector128<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - Vector128<long>.Count - (index - Vector256<long>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<long>.Count + Vector128<long>.Count; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - (index - Vector256<long>.Count - Vector128<long>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleNativeOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<sbyte> result = Vector512.ShuffleNative(
                Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((sbyte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48)
            );

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector128<sbyte>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<sbyte>.Count; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector256<sbyte>.Count - (index - Vector128<sbyte>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<sbyte>.Count; index < Vector512<sbyte>.Count - Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - Vector128<sbyte>.Count - (index - Vector256<sbyte>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<sbyte>.Count + Vector128<sbyte>.Count; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - (index - Vector256<sbyte>.Count - Vector128<sbyte>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleNativeOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<float> result = Vector512.ShuffleNative(
                Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)
            );

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)(Vector128<float>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<float>.Count; index < Vector256<float>.Count; index++)
            {
                Assert.Equal((float)(Vector256<float>.Count - (index - Vector128<float>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<float>.Count; index < Vector512<float>.Count - Vector128<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - Vector128<float>.Count - (index - Vector256<float>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<float>.Count + Vector128<float>.Count; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - (index - Vector256<float>.Count - Vector128<float>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleNativeOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<ushort> result = Vector512.ShuffleNative(
                Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((ushort)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24)
            );

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector128<ushort>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<ushort>.Count; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector256<ushort>.Count - (index - Vector128<ushort>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<ushort>.Count; index < Vector512<ushort>.Count - Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - Vector128<ushort>.Count - (index - Vector256<ushort>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<ushort>.Count + Vector128<ushort>.Count; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - (index - Vector256<ushort>.Count - Vector128<ushort>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleNativeOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<uint> result = Vector512.ShuffleNative(
                Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((uint)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)
            );

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector128<uint>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<uint>.Count; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector256<uint>.Count - (index - Vector128<uint>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<uint>.Count; index < Vector512<uint>.Count - Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - Vector128<uint>.Count - (index - Vector256<uint>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<uint>.Count + Vector128<uint>.Count; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - (index - Vector256<uint>.Count - Vector128<uint>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleNativeOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<ulong> result = Vector512.ShuffleNative(
                Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((ulong)1, 0, 3, 2, 5, 4, 7, 6)
            );

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector128<ulong>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<ulong>.Count; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector256<ulong>.Count - (index - Vector128<ulong>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<ulong>.Count; index < Vector512<ulong>.Count - Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - Vector128<ulong>.Count - (index - Vector256<ulong>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<ulong>.Count + Vector128<ulong>.Count; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - (index - Vector256<ulong>.Count - Vector128<ulong>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleNativeOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<byte> result = Vector512.ShuffleNative(
                Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((byte)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32)
            );

            for (int index = 0; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector256<byte>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<byte>.Count; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - (index - Vector256<byte>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleNativeOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<double> result = Vector512.ShuffleNative(
                Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)3, 2, 1, 0, 7, 6, 5, 4)
            );

            for (int index = 0; index < Vector256<double>.Count; index++)
            {
                Assert.Equal((double)(Vector256<double>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<double>.Count; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - (index - Vector256<double>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleNativeOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<short> result = Vector512.ShuffleNative(
                Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((short)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16)
            );

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)(Vector256<short>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<short>.Count; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - (index - Vector256<short>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleNativeOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<int> result = Vector512.ShuffleNative(
                Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)
            );

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)(Vector256<int>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<int>.Count; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - (index - Vector256<int>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleNativeOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<long> result = Vector512.ShuffleNative(
                Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)3, 2, 1, 0, 7, 6, 5, 4)
            );

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)(Vector256<long>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<long>.Count; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - (index - Vector256<long>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleNativeOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<sbyte> result = Vector512.ShuffleNative(
                Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((sbyte)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32)
            );

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector256<sbyte>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<sbyte>.Count; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - (index - Vector256<sbyte>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleNativeOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<float> result = Vector512.ShuffleNative(
                Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)
            );

            for (int index = 0; index < Vector256<float>.Count; index++)
            {
                Assert.Equal((float)(Vector256<float>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<float>.Count; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - (index - Vector256<float>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleNativeOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<ushort> result = Vector512.ShuffleNative(
                Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((ushort)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16)
            );

            for (int index = 0; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector256<ushort>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<ushort>.Count; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - (index - Vector256<ushort>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleNativeOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<uint> result = Vector512.ShuffleNative(
                Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((uint)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)
            );

            for (int index = 0; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector256<uint>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<uint>.Count; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - (index - Vector256<uint>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleNativeOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<ulong> result = Vector512.ShuffleNative(
                Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((ulong)3, 2, 1, 0, 7, 6, 5, 4)
            );

            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector256<ulong>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<ulong>.Count; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - (index - Vector256<ulong>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleNativeOneInputWithLocalIndicesTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<byte> indices = Vector512.Create((byte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<byte> result = Vector512.ShuffleNative(vector, indices);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleNativeOneInputWithLocalIndicesTest()
        {
            Vector512<double> vector = Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> indices = Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<double> result = Vector512.ShuffleNative(vector, indices);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleNativeOneInputWithLocalIndicesTest()
        {
            Vector512<short> vector = Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<short> indices = Vector512.Create((short)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<short> result = Vector512.ShuffleNative(vector, indices);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleNativeOneInputWithLocalIndicesTest()
        {
            Vector512<int> vector = Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> indices = Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<int> result = Vector512.ShuffleNative(vector, indices);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleNativeOneInputWithLocalIndicesTest()
        {
            Vector512<long> vector = Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> indices = Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<long> result = Vector512.ShuffleNative(vector, indices);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleNativeOneInputWithLocalIndicesTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<sbyte> indices = Vector512.Create((sbyte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<sbyte> result = Vector512.ShuffleNative(vector, indices);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleNativeOneInputWithLocalIndicesTest()
        {
            Vector512<float> vector = Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> indices = Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<float> result = Vector512.ShuffleNative(vector, indices);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleNativeOneInputWithLocalIndicesTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<ushort> indices = Vector512.Create((ushort)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<ushort> result = Vector512.ShuffleNative(vector, indices);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleNativeOneInputWithLocalIndicesTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<uint> indices = Vector512.Create((uint)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<uint> result = Vector512.ShuffleNative(vector, indices);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleNativeOneInputWithLocalIndicesTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<ulong> indices = Vector512.Create((ulong)7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<ulong> result = Vector512.ShuffleNative(vector, indices);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleNativeOneInputWithZeroIndicesTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<byte> result = Vector512.ShuffleNative(vector, Vector512<byte>.Zero);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleNativeOneInputWithZeroIndicesTest()
        {
            Vector512<double> vector = Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<double> result = Vector512.ShuffleNative(vector, Vector512<long>.Zero);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleNativeOneInputWithZeroIndicesTest()
        {
            Vector512<short> vector = Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<short> result = Vector512.ShuffleNative(vector, Vector512<short>.Zero);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleNativeOneInputWithZeroIndicesTest()
        {
            Vector512<int> vector = Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> result = Vector512.ShuffleNative(vector, Vector512<int>.Zero);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleNativeOneInputWithZeroIndicesTest()
        {
            Vector512<long> vector = Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> result = Vector512.ShuffleNative(vector, Vector512<long>.Zero);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleNativeOneInputWithZeroIndicesTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<sbyte> result = Vector512.ShuffleNative(vector, Vector512<sbyte>.Zero);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleNativeOneInputWithZeroIndicesTest()
        {
            Vector512<float> vector = Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<float> result = Vector512.ShuffleNative(vector, Vector512<int>.Zero);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleNativeOneInputWithZeroIndicesTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<ushort> result = Vector512.ShuffleNative(vector, Vector512<ushort>.Zero);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleNativeOneInputWithZeroIndicesTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<uint> result = Vector512.ShuffleNative(vector, Vector512<uint>.Zero);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleNativeOneInputWithZeroIndicesTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<ulong> result = Vector512.ShuffleNative(vector, Vector512<ulong>.Zero);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)1, result.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512ByteStoreTest()
        {
            byte* value = stackalloc byte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (byte)(index);
            }

            Vector512.Create((byte)0x1).Store(value);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleStoreTest()
        {
            double* value = stackalloc double[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512.Create((double)0x1).Store(value);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512Int16StoreTest()
        {
            short* value = stackalloc short[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (short)(index);
            }

            Vector512.Create((short)0x1).Store(value);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512Int32StoreTest()
        {
            int* value = stackalloc int[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512.Create((int)0x1).Store(value);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512Int64StoreTest()
        {
            long* value = stackalloc long[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512.Create((long)0x1).Store(value);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512NIntStoreTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((nint)0x1).Store(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            else
            {
                nint* value = stackalloc nint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((nint)0x1).Store(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector512NUIntStoreTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512.Create((nuint)0x1).Store(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512.Create((nuint)0x1).Store(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector512SByteStoreTest()
        {
            sbyte* value = stackalloc sbyte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (sbyte)(index);
            }

            Vector512.Create((sbyte)0x1).Store(value);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512SingleStoreTest()
        {
            float* value = stackalloc float[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512.Create((float)0x1).Store(value);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16StoreTest()
        {
            ushort* value = stackalloc ushort[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (ushort)(index);
            }

            Vector512.Create((ushort)0x1).Store(value);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32StoreTest()
        {
            uint* value = stackalloc uint[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = (uint)(index);
            }

            Vector512.Create((uint)0x1).Store(value);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64StoreTest()
        {
            ulong* value = stackalloc ulong[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = (ulong)(index);
            }

            Vector512.Create((ulong)0x1).Store(value);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512ByteStoreAlignedTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (byte)(index);
                }

                Vector512.Create((byte)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<byte>.Count; index++)
                {
                    Assert.Equal((byte)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleStoreAlignedTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((double)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<double>.Count; index++)
                {
                    Assert.Equal((double)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int16StoreAlignedTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (short)(index);
                }

                Vector512.Create((short)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<short>.Count; index++)
                {
                    Assert.Equal((short)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int32StoreAlignedTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((int)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<int>.Count; index++)
                {
                    Assert.Equal((int)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int64StoreAlignedTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((long)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<long>.Count; index++)
                {
                    Assert.Equal((long)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NIntStoreAlignedTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = index;
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = index;
                    }
                }

                Vector512.Create((nint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NUIntStoreAlignedTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }

                Vector512.Create((nuint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SByteStoreAlignedTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (sbyte)(index);
                }

                Vector512.Create((sbyte)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<sbyte>.Count; index++)
                {
                    Assert.Equal((sbyte)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SingleStoreAlignedTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((float)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<float>.Count; index++)
                {
                    Assert.Equal((float)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16StoreAlignedTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (ushort)(index);
                }

                Vector512.Create((ushort)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<ushort>.Count; index++)
                {
                    Assert.Equal((ushort)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32StoreAlignedTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (uint)(index);
                }

                Vector512.Create((uint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<uint>.Count; index++)
                {
                    Assert.Equal((uint)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64StoreAlignedTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (ulong)(index);
                }

                Vector512.Create((ulong)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<ulong>.Count; index++)
                {
                    Assert.Equal((ulong)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512ByteStoreAlignedNonTemporalTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (byte)(index);
                }

                Vector512.Create((byte)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<byte>.Count; index++)
                {
                    Assert.Equal((byte)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleStoreAlignedNonTemporalTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((double)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<double>.Count; index++)
                {
                    Assert.Equal((double)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int16StoreAlignedNonTemporalTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (short)(index);
                }

                Vector512.Create((short)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<short>.Count; index++)
                {
                    Assert.Equal((short)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int32StoreAlignedNonTemporalTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((int)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<int>.Count; index++)
                {
                    Assert.Equal((int)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int64StoreAlignedNonTemporalTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((long)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<long>.Count; index++)
                {
                    Assert.Equal((long)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NIntStoreAlignedNonTemporalTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = index;
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = index;
                    }
                }

                Vector512.Create((nint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NUIntStoreAlignedNonTemporalTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }

                Vector512.Create((nuint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SByteStoreAlignedNonTemporalTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (sbyte)(index);
                }

                Vector512.Create((sbyte)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<sbyte>.Count; index++)
                {
                    Assert.Equal((sbyte)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SingleStoreAlignedNonTemporalTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((float)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<float>.Count; index++)
                {
                    Assert.Equal((float)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16StoreAlignedNonTemporalTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (ushort)(index);
                }

                Vector512.Create((ushort)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<ushort>.Count; index++)
                {
                    Assert.Equal((ushort)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32StoreAlignedNonTemporalTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (uint)(index);
                }

                Vector512.Create((uint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<uint>.Count; index++)
                {
                    Assert.Equal((uint)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64StoreAlignedNonTemporalTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (ulong)(index);
                }

                Vector512.Create((ulong)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<ulong>.Count; index++)
                {
                    Assert.Equal((ulong)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512ByteStoreUnsafeTest()
        {
            byte* value = stackalloc byte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (byte)(index);
            }

            Vector512.Create((byte)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleStoreUnsafeTest()
        {
            double* value = stackalloc double[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512.Create((double)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512Int16StoreUnsafeTest()
        {
            short* value = stackalloc short[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (short)(index);
            }

            Vector512.Create((short)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512Int32StoreUnsafeTest()
        {
            int* value = stackalloc int[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512.Create((int)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512Int64StoreUnsafeTest()
        {
            long* value = stackalloc long[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512.Create((long)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512NIntStoreUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((nint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            else
            {
                nint* value = stackalloc nint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((nint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector512NUIntStoreUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512.Create((nuint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512.Create((nuint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector512SByteStoreUnsafeTest()
        {
            sbyte* value = stackalloc sbyte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (sbyte)(index);
            }

            Vector512.Create((sbyte)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512SingleStoreUnsafeTest()
        {
            float* value = stackalloc float[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512.Create((float)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16StoreUnsafeTest()
        {
            ushort* value = stackalloc ushort[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (ushort)(index);
            }

            Vector512.Create((ushort)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32StoreUnsafeTest()
        {
            uint* value = stackalloc uint[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = (uint)(index);
            }

            Vector512.Create((uint)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64StoreUnsafeTest()
        {
            ulong* value = stackalloc ulong[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = (ulong)(index);
            }

            Vector512.Create((ulong)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512ByteStoreUnsafeIndexTest()
        {
            byte* value = stackalloc byte[64 + 1];

            for (int index = 0; index < 64 + 1; index++)
            {
                value[index] = (byte)(index);
            }

            Vector512.Create((byte)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleStoreUnsafeIndexTest()
        {
            double* value = stackalloc double[8 + 1];

            for (int index = 0; index < 8 + 1; index++)
            {
                value[index] = index;
            }

            Vector512.Create((double)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512Int16StoreUnsafeIndexTest()
        {
            short* value = stackalloc short[32 + 1];

            for (int index = 0; index < 32 + 1; index++)
            {
                value[index] = (short)(index);
            }

            Vector512.Create((short)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512Int32StoreUnsafeIndexTest()
        {
            int* value = stackalloc int[16 + 1];

            for (int index = 0; index < 16 + 1; index++)
            {
                value[index] = index;
            }

            Vector512.Create((int)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512Int64StoreUnsafeIndexTest()
        {
            long* value = stackalloc long[8 + 1];

            for (int index = 0; index < 8 + 1; index++)
            {
                value[index] = index;
            }

            Vector512.Create((long)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512NIntStoreUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[8 + 1];

                for (int index = 0; index < 8 + 1; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((nint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index + 1]);
                }
            }
            else
            {
                nint* value = stackalloc nint[16 + 1];

                for (int index = 0; index < 16 + 1; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((nint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index + 1]);
                }
            }
        }

        [Fact]
        public unsafe void Vector512NUIntStoreUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[8 + 1];

                for (int index = 0; index < 8 + 1; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512.Create((nuint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index + 1]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[16 + 1];

                for (int index = 0; index < 16 + 1; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512.Create((nuint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index + 1]);
                }
            }
        }

        [Fact]
        public unsafe void Vector512SByteStoreUnsafeIndexTest()
        {
            sbyte* value = stackalloc sbyte[64 + 1];

            for (int index = 0; index < 64 + 1; index++)
            {
                value[index] = (sbyte)(index);
            }

            Vector512.Create((sbyte)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512SingleStoreUnsafeIndexTest()
        {
            float* value = stackalloc float[16 + 1];

            for (int index = 0; index < 16 + 1; index++)
            {
                value[index] = index;
            }

            Vector512.Create((float)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16StoreUnsafeIndexTest()
        {
            ushort* value = stackalloc ushort[32 + 1];

            for (int index = 0; index < 32 + 1; index++)
            {
                value[index] = (ushort)(index);
            }

            Vector512.Create((ushort)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32StoreUnsafeIndexTest()
        {
            uint* value = stackalloc uint[16 + 1];

            for (int index = 0; index < 16 + 1; index++)
            {
                value[index] = (uint)(index);
            }

            Vector512.Create((uint)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64StoreUnsafeIndexTest()
        {
            ulong* value = stackalloc ulong[8 + 1];

            for (int index = 0; index < 8 + 1; index++)
            {
                value[index] = (ulong)(index);
            }

            Vector512.Create((ulong)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index + 1]);
            }
        }

        [Fact]
        public void Vector512ByteSumTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)0x01);
            Assert.Equal((byte)64, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512DoubleSumTest()
        {
            Vector512<double> vector = Vector512.Create((double)0x01);
            Assert.Equal(8.0, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512Int16SumTest()
        {
            Vector512<short> vector = Vector512.Create((short)0x01);
            Assert.Equal((short)32, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512Int32SumTest()
        {
            Vector512<int> vector = Vector512.Create((int)0x01);
            Assert.Equal((int)16, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512Int64SumTest()
        {
            Vector512<long> vector = Vector512.Create((long)0x01);
            Assert.Equal((long)8, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512NIntSumTest()
        {
            Vector512<nint> vector = Vector512.Create((nint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((nint)8, Vector512.Sum(vector));
            }
            else
            {
                Assert.Equal((nint)16, Vector512.Sum(vector));
            }
        }

        [Fact]
        public void Vector512NUIntSumTest()
        {
            Vector512<nuint> vector = Vector512.Create((nuint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((nuint)8, Vector512.Sum(vector));
            }
            else
            {
                Assert.Equal((nuint)16, Vector512.Sum(vector));
            }
        }

        [Fact]
        public void Vector512SByteSumTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)0x01);
            Assert.Equal((sbyte)64, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512SingleSumTest()
        {
            Vector512<float> vector = Vector512.Create((float)0x01);
            Assert.Equal(16.0f, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512UInt16SumTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)0x01);
            Assert.Equal((ushort)32, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512UInt32SumTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)0x01);
            Assert.Equal((uint)16, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512UInt64SumTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)0x01);
            Assert.Equal((ulong)8, Vector512.Sum(vector));
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
        [InlineData(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1)]
        [InlineData(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1)]
        [InlineData(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)]
        [InlineData(0, 0, 50, 430, -64, 0, int.MaxValue, int.MinValue, 0, 0, 50, 430, -64, 0, int.MaxValue, int.MinValue)]
        public void Vector512Int32IndexerTest(params int[] values)
        {
            var vector = Vector512.Create(values);

            Assert.Equal(vector[0], values[0]);
            Assert.Equal(vector[1], values[1]);
            Assert.Equal(vector[2], values[2]);
            Assert.Equal(vector[3], values[3]);
            Assert.Equal(vector[4], values[4]);
            Assert.Equal(vector[5], values[5]);
            Assert.Equal(vector[6], values[6]);
            Assert.Equal(vector[7], values[7]);
        }

        [Theory]
        [InlineData(0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L)]
        [InlineData(1L, 1L, 1L, 1L, 1L, 1L, 1L, 1L)]
        [InlineData(0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L)]
        [InlineData(0L, 0L, 50L, 430L, -64L, 0L, long.MaxValue, long.MinValue)]
        public void Vector512Int64IndexerTest(params long[] values)
        {
            var vector = Vector512.Create(values);

            Assert.Equal(vector[0], values[0]);
            Assert.Equal(vector[1], values[1]);
            Assert.Equal(vector[2], values[2]);
            Assert.Equal(vector[3], values[3]);
        }

        [Fact]
        public void Vector512DoubleEqualsNaNTest()
        {
            Vector512<double> nan = Vector512.Create(double.NaN);
            Assert.True(nan.Equals(nan));
        }

        [Fact]
        public void Vector512SingleEqualsNaNTest()
        {
            Vector512<float> nan = Vector512.Create(float.NaN);
            Assert.True(nan.Equals(nan));
        }

        [Fact]
        public void Vector512DoubleEqualsNonCanonicalNaNTest()
        {
            // max 8 bit exponent, just under half max mantissa
            var snan = BitConverter.UInt64BitsToDouble(0x7FF7_FFFF_FFFF_FFFF);
            var nans = new double[]
            {
                double.CopySign(double.NaN, -0.0), // -qnan same as double.NaN
                double.CopySign(double.NaN, +0.0), // +qnan
                double.CopySign(snan, -0.0),       // -snan
                double.CopySign(snan, +0.0),       // +snan
            };

            // all Vector<double> NaNs .Equals compare the same, but == compare as different
            foreach (var i in nans)
            {
                foreach (var j in nans)
                {
                    Assert.True(Vector512.Create(i).Equals(Vector512.Create(j)));
                    Assert.False(Vector512.Create(i) == Vector512.Create(j));
                }
            }
        }

        [Fact]
        public void Vector512SingleEqualsNonCanonicalNaNTest()
        {
            // max 11 bit exponent, just under half max mantissa
            var snan = BitConverter.UInt32BitsToSingle(0x7FBF_FFFF);
            var nans = new float[]
            {
                float.CopySign(float.NaN, -0.0f), // -qnan same as float.NaN
                float.CopySign(float.NaN, +0.0f), // +qnan
                float.CopySign(snan, -0.0f),      // -snan
                float.CopySign(snan, +0.0f),      // +snan
            };

            // all Vector<float> NaNs .Equals compare the same, but == compare as different
            foreach (var i in nans)
            {
                foreach (var j in nans)
                {
                    Assert.True(Vector512.Create(i).Equals(Vector512.Create(j)));
                    Assert.False(Vector512.Create(i) == Vector512.Create(j));
                }
            }
        }

        [Fact]
        public void Vector512SingleCreateFromArrayTest()
        {
            float[] array = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f, 17.0f];
            Vector512<float> vector = Vector512.Create(array);
            Assert.Equal(Vector512.Create(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f), vector);
        }

        [Fact]
        public void Vector512SingleCreateFromArrayOffsetTest()
        {
            float[] array = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f, 17.0f];
            Vector512<float> vector = Vector512.Create(array, 1);
            Assert.Equal(Vector512.Create(2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f, 17.0f), vector);
        }

        [Fact]
        public void Vector512SingleCopyToTest()
        {
            float[] array = new float[16];
            Vector512.Create(2.0f).CopyTo(array);
            Assert.True(array.AsSpan().SequenceEqual([2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f]));
        }

        [Fact]
        public void Vector512SingleCopyToOffsetTest()
        {
            float[] array = new float[17];
            Vector512.Create(2.0f).CopyTo(array, 1);
            Assert.True(array.AsSpan().SequenceEqual([0.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f]));
        }

        [Fact]
        public void Vector512SByteAbs_MinValue()
        {
            Vector512<sbyte> vector = Vector512.Create(sbyte.MinValue);
            Vector512<sbyte> abs = Vector512.Abs(vector);
            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal(sbyte.MinValue, vector.GetElement(index));
            }
        }

        [Fact]
        public void IsSupportedByte() => TestIsSupported<byte>();

        [Fact]
        public void IsSupportedDouble() => TestIsSupported<double>();

        [Fact]
        public void IsSupportedInt16() => TestIsSupported<short>();

        [Fact]
        public void IsSupportedInt32() => TestIsSupported<int>();

        [Fact]
        public void IsSupportedInt64() => TestIsSupported<long>();

        [Fact]
        public void IsSupportedIntPtr() => TestIsSupported<nint>();

        [Fact]
        public void IsSupportedSByte() => TestIsSupported<sbyte>();

        [Fact]
        public void IsSupportedSingle() => TestIsSupported<float>();

        [Fact]
        public void IsSupportedUInt16() => TestIsSupported<ushort>();

        [Fact]
        public void IsSupportedUInt32() => TestIsSupported<uint>();

        [Fact]
        public void IsSupportedUInt64() => TestIsSupported<ulong>();

        [Fact]
        public void IsSupportedUIntPtr() => TestIsSupported<nuint>();

        private static void TestIsSupported<T>()
            where T : struct
        {
            Assert.True(Vector512<T>.IsSupported);

            MethodInfo methodInfo = typeof(Vector512<T>).GetProperty("IsSupported", BindingFlags.Public | BindingFlags.Static).GetMethod;
            Assert.True((bool)methodInfo.Invoke(null, null));
        }

        [Fact]
        public void IsNotSupportedBoolean() => TestIsNotSupported<bool>();

        [Fact]
        public void IsNotSupportedChar() => TestIsNotSupported<char>();

        [Fact]
        public void IsNotSupportedHalf() => TestIsNotSupported<Half>();

        [Fact]
        public void IsNotSupportedInt128() => TestIsNotSupported<Int128>();

        [Fact]
        public void IsNotSupportedUInt128() => TestIsNotSupported<UInt128>();

        private static void TestIsNotSupported<T>()
            where T : struct
        {
            Assert.False(Vector512<T>.IsSupported);

            MethodInfo methodInfo = typeof(Vector512<T>).GetProperty("IsSupported", BindingFlags.Public | BindingFlags.Static).GetMethod;
            Assert.False((bool)methodInfo.Invoke(null, null));
        }

        [Fact]
        public void GetIndicesByteTest() => TestGetIndices<byte>();

        [Fact]
        public void GetIndicesDoubleTest() => TestGetIndices<double>();

        [Fact]
        public void GetIndicesInt16Test() => TestGetIndices<short>();

        [Fact]
        public void GetIndicesInt32Test() => TestGetIndices<int>();

        [Fact]
        public void GetIndicesInt64Test() => TestGetIndices<long>();

        [Fact]
        public void GetIndicesNIntTest() => TestGetIndices<nint>();

        [Fact]
        public void GetIndicesNUIntTest() => TestGetIndices<nuint>();

        [Fact]
        public void GetIndicesSByteTest() => TestGetIndices<sbyte>();

        [Fact]
        public void GetIndicesSingleTest() => TestGetIndices<float>();

        [Fact]
        public void GetIndicesUInt16Test() => TestGetIndices<ushort>();

        [Fact]
        public void GetIndicesUInt32Test() => TestGetIndices<uint>();

        [Fact]
        public void GetIndicesUInt64Test() => TestGetIndices<ulong>();

        private static void TestGetIndices<T>()
            where T : INumber<T>
        {
            Vector512<T> indices = Vector512<T>.Indices;

            for (int index = 0; index < Vector512<T>.Count; index++)
            {
                Assert.Equal(T.CreateTruncating(index), indices.GetElement(index));
            }
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(3, 3)]
        [InlineData(63, unchecked((byte)(-1)))]
        public void CreateSequenceByteTest(byte start, byte step) => TestCreateSequence<byte>(start, step);

        [Theory]
        [InlineData(0.0, +2.0)]
        [InlineData(3.0, +3.0)]
        [InlineData(7.0, -1.0)]
        public void CreateSequenceDoubleTest(double start, double step) => TestCreateSequence<double>(start, step);

        [Theory]
        [InlineData(0, +2)]
        [InlineData(3, +3)]
        [InlineData(31, -1)]
        public void CreateSequenceInt16Test(short start, short step) => TestCreateSequence<short>(start, step);

        [Theory]
        [InlineData(0, +2)]
        [InlineData(3, +3)]
        [InlineData(15, -1)]
        public void CreateSequenceInt32Test(int start, int step) => TestCreateSequence<int>(start, step);

        [Theory]
        [InlineData(0, +2)]
        [InlineData(3, +3)]
        [InlineData(31, -1)]
        public void CreateSequenceInt64Test(long start, long step) => TestCreateSequence<long>(start, step);

        [Theory]
        [InlineData(0, +2)]
        [InlineData(3, +3)]
        [InlineData(63, -1)]
        public void CreateSequenceSByteTest(sbyte start, sbyte step) => TestCreateSequence<sbyte>(start, step);

        [Theory]
        [InlineData(0.0f, +2.0f)]
        [InlineData(3.0f, +3.0f)]
        [InlineData(15.0f, -1.0f)]
        public void CreateSequenceSingleTest(float start, float step) => TestCreateSequence<float>(start, step);

        [Theory]
        [InlineData(0, 2)]
        [InlineData(3, 3)]
        [InlineData(31, unchecked((ushort)(-1)))]
        public void CreateSequenceUInt16Test(ushort start, ushort step) => TestCreateSequence<ushort>(start, step);

        [Theory]
        [InlineData(0, 2)]
        [InlineData(3, 3)]
        [InlineData(15, unchecked((uint)(-1)))]
        public void CreateSequenceUInt32Test(uint start, uint step) => TestCreateSequence<uint>(start, step);

        [Theory]
        [InlineData(0, 2)]
        [InlineData(3, 3)]
        [InlineData(7, unchecked((ulong)(-1)))]
        public void CreateSequenceUInt64Test(ulong start, ulong step) => TestCreateSequence<ulong>(start, step);

        private static void TestCreateSequence<T>(T start, T step)
            where T : INumber<T>
        {
            Vector512<T> sequence = Vector512.CreateSequence(start, step);
            T expected = start;

            for (int index = 0; index < Vector512<T>.Count; index++)
            {
                Assert.Equal(expected, sequence.GetElement(index));
                expected += step;
            }
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.CosDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void CosDoubleTest(double value, double expectedResult, double variance)
        {
            Vector512<double> actualResult = Vector512.Cos(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.CosSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void CosSingleTest(float value, float expectedResult, float variance)
        {
            Vector512<float> actualResult = Vector512.Cos(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.ExpDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void ExpDoubleTest(double value, double expectedResult, double variance)
        {
            Vector512<double> actualResult = Vector512.Exp(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.ExpSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void ExpSingleTest(float value, float expectedResult, float variance)
        {
            Vector512<float> actualResult = Vector512.Exp(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.LogDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void LogDoubleTest(double value, double expectedResult, double variance)
        {
            Vector512<double> actualResult = Vector512.Log(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.LogSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void LogSingleTest(float value, float expectedResult, float variance)
        {
            Vector512<float> actualResult = Vector512.Log(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.Log2Double), MemberType = typeof(GenericMathTestMemberData))]
        public void Log2DoubleTest(double value, double expectedResult, double variance)
        {
            Vector512<double> actualResult = Vector512.Log2(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.Log2Single), MemberType = typeof(GenericMathTestMemberData))]
        public void Log2SingleTest(float value, float expectedResult, float variance)
        {
            Vector512<float> actualResult = Vector512.Log2(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.FusedMultiplyAddDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void FusedMultiplyAddDoubleTest(double left, double right, double addend, double expectedResult)
        {
            AssertEqual(Vector512.Create(expectedResult), Vector512.FusedMultiplyAdd(Vector512.Create(left), Vector512.Create(right), Vector512.Create(addend)), Vector512<double>.Zero);
            AssertEqual(Vector512.Create(double.MultiplyAddEstimate(left, right, addend)), Vector512.MultiplyAddEstimate(Vector512.Create(left), Vector512.Create(right), Vector512.Create(addend)), Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.FusedMultiplyAddSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void FusedMultiplyAddSingleTest(float left, float right, float addend, float expectedResult)
        {
            AssertEqual(Vector512.Create(expectedResult), Vector512.FusedMultiplyAdd(Vector512.Create(left), Vector512.Create(right), Vector512.Create(addend)), Vector512<float>.Zero);
            AssertEqual(Vector512.Create(float.MultiplyAddEstimate(left, right, addend)), Vector512.MultiplyAddEstimate(Vector512.Create(left), Vector512.Create(right), Vector512.Create(addend)), Vector512<float>.Zero);
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/100368")]
        public void ConvertToInt32Test()
        {
            Assert.Equal(Vector512.Create(float.ConvertToInteger<int>(float.MinValue)), Vector512.ConvertToInt32(Vector512.Create(float.MinValue)));
            Assert.Equal(Vector512.Create(float.ConvertToInteger<int>(2.6f)), Vector512.ConvertToInt32(Vector512.Create(2.6f)));
            Assert.Equal(Vector512.Create(float.ConvertToInteger<int>(float.MaxValue)), Vector512.ConvertToInt32(Vector512.Create(float.MaxValue)));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/100368")]
        public void ConvertToInt32NativeTest()
        {
            Assert.Equal(Vector512.Create(float.ConvertToIntegerNative<int>(float.MinValue)), Vector512.ConvertToInt32Native(Vector512.Create(float.MinValue)));
            Assert.Equal(Vector512.Create(float.ConvertToIntegerNative<int>(2.6f)), Vector512.ConvertToInt32Native(Vector512.Create(2.6f)));
            Assert.Equal(Vector512.Create(float.ConvertToIntegerNative<int>(float.MaxValue)), Vector512.ConvertToInt32Native(Vector512.Create(float.MaxValue)));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/100368")]
        public void ConvertToInt64Test()
        {
            Assert.Equal(Vector512.Create(double.ConvertToInteger<long>(double.MinValue)), Vector512.ConvertToInt64(Vector512.Create(double.MinValue)));
            Assert.Equal(Vector512.Create(double.ConvertToInteger<long>(2.6)), Vector512.ConvertToInt64(Vector512.Create(2.6)));
            Assert.Equal(Vector512.Create(double.ConvertToInteger<long>(double.MaxValue)), Vector512.ConvertToInt64(Vector512.Create(double.MaxValue)));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/100368")]
        public void ConvertToInt64NativeTest()
        {
            Assert.Equal(Vector512.Create(double.ConvertToIntegerNative<long>(double.MinValue)), Vector512.ConvertToInt64Native(Vector512.Create(double.MinValue)));
            Assert.Equal(Vector512.Create(double.ConvertToIntegerNative<long>(2.6)), Vector512.ConvertToInt64Native(Vector512.Create(2.6)));

            if (Environment.Is64BitProcess)
            {
                // This isn't accelerated on all 32-bit systems today and may fallback to ConvertToInteger behavior
                Assert.Equal(Vector512.Create(double.ConvertToIntegerNative<long>(double.MaxValue)), Vector512.ConvertToInt64Native(Vector512.Create(double.MaxValue)));
            }
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/100368")]
        public void ConvertToUInt32Test()
        {
            Assert.Equal(Vector512.Create(float.ConvertToInteger<uint>(float.MinValue)), Vector512.ConvertToUInt32(Vector512.Create(float.MinValue)));
            Assert.Equal(Vector512.Create(float.ConvertToInteger<uint>(2.6f)), Vector512.ConvertToUInt32(Vector512.Create(2.6f)));
            Assert.Equal(Vector512.Create(float.ConvertToInteger<uint>(float.MaxValue)), Vector512.ConvertToUInt32(Vector512.Create(float.MaxValue)));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/100368")]
        public void ConvertToUInt32NativeTest()
        {
            Assert.Equal(Vector512.Create(float.ConvertToIntegerNative<uint>(float.MinValue)), Vector512.ConvertToUInt32Native(Vector512.Create(float.MinValue)));
            Assert.Equal(Vector512.Create(float.ConvertToIntegerNative<uint>(2.6f)), Vector512.ConvertToUInt32Native(Vector512.Create(2.6f)));
            Assert.Equal(Vector512.Create(float.ConvertToIntegerNative<uint>(float.MaxValue)), Vector512.ConvertToUInt32Native(Vector512.Create(float.MaxValue)));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/100368")]
        public void ConvertToUInt64Test()
        {
            Assert.Equal(Vector512.Create(double.ConvertToInteger<ulong>(double.MinValue)), Vector512.ConvertToUInt64(Vector512.Create(double.MinValue)));
            Assert.Equal(Vector512.Create(double.ConvertToInteger<ulong>(2.6)), Vector512.ConvertToUInt64(Vector512.Create(2.6)));
            Assert.Equal(Vector512.Create(double.ConvertToInteger<ulong>(double.MaxValue)), Vector512.ConvertToUInt64(Vector512.Create(double.MaxValue)));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/100368")]
        public void ConvertToUInt64NativeTest()
        {
            if (Environment.Is64BitProcess)
            {
                // This isn't accelerated on all 32-bit systems today and may fallback to ConvertToInteger behavior
                Assert.Equal(Vector512.Create(double.ConvertToIntegerNative<ulong>(double.MinValue)), Vector512.ConvertToUInt64Native(Vector512.Create(double.MinValue)));
            }

            Assert.Equal(Vector512.Create(double.ConvertToIntegerNative<ulong>(2.6)), Vector512.ConvertToUInt64Native(Vector512.Create(2.6)));
            Assert.Equal(Vector512.Create(double.ConvertToIntegerNative<ulong>(double.MaxValue)), Vector512.ConvertToUInt64Native(Vector512.Create(double.MaxValue)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.ClampDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void ClampDoubleTest(double x, double min, double max, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.Clamp(Vector512.Create(x), Vector512.Create(min), Vector512.Create(max));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.ClampSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void ClampSingleTest(float x, float min, float max, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.Clamp(Vector512.Create(x), Vector512.Create(min), Vector512.Create(max));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.CopySignDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void CopySignDoubleTest(double x, double y, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.CopySign(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.CopySignSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void CopySignSingleTest(float x, float y, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.CopySign(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.DegreesToRadiansDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void DegreesToRadiansDoubleTest(double value, double expectedResult, double variance)
        {
            Vector512<double> actualResult1 = Vector512.DegreesToRadians(Vector512.Create(-value));
            AssertEqual(Vector512.Create(-expectedResult), actualResult1, Vector512.Create(variance));

            Vector512<double> actualResult2 = Vector512.DegreesToRadians(Vector512.Create(+value));
            AssertEqual(Vector512.Create(+expectedResult), actualResult2, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.DegreesToRadiansSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void DegreesToRadiansSingleTest(float value, float expectedResult, float variance)
        {
            AssertEqual(Vector512.Create(-expectedResult), Vector512.DegreesToRadians(Vector512.Create(-value)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(+expectedResult), Vector512.DegreesToRadians(Vector512.Create(+value)), Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.HypotDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void HypotDoubleTest(double x, double y, double expectedResult, double variance)
        {
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(-x), Vector512.Create(-y)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(-x), Vector512.Create(+y)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(+x), Vector512.Create(-y)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(+x), Vector512.Create(+y)), Vector512.Create(variance));

            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(-y), Vector512.Create(-x)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(-y), Vector512.Create(+x)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(+y), Vector512.Create(-x)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(+y), Vector512.Create(+x)), Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.HypotSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void HypotSingleTest(float x, float y, float expectedResult, float variance)
        {
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(-x), Vector512.Create(-y)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(-x), Vector512.Create(+y)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(+x), Vector512.Create(-y)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(+x), Vector512.Create(+y)), Vector512.Create(variance));

            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(-y), Vector512.Create(-x)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(-y), Vector512.Create(+x)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(+y), Vector512.Create(-x)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(expectedResult), Vector512.Hypot(Vector512.Create(+y), Vector512.Create(+x)), Vector512.Create(variance));
        }

        private void IsEvenInteger<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsEvenInteger(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsEvenInteger(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsEvenIntegerByteTest(byte value) => IsEvenInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsEvenIntegerDoubleTest(double value) => IsEvenInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsEvenIntegerInt16Test(short value) => IsEvenInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsEvenIntegerInt32Test(int value) => IsEvenInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsEvenIntegerInt64Test(long value) => IsEvenInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsEvenIntegerSByteTest(sbyte value) => IsEvenInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsEvenIntegerSingleTest(float value) => IsEvenInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsEvenIntegerUInt16Test(ushort value) => IsEvenInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsEvenIntegerUInt32Test(uint value) => IsEvenInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsEvenIntegerUInt64Test(ulong value) => IsEvenInteger(value);

        private void IsFinite<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsFinite(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsFinite(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsFiniteByteTest(byte value) => IsFinite(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsFiniteDoubleTest(double value) => IsFinite(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsFiniteInt16Test(short value) => IsFinite(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsFiniteInt32Test(int value) => IsFinite(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsFiniteInt64Test(long value) => IsFinite(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsFiniteSByteTest(sbyte value) => IsFinite(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsFiniteSingleTest(float value) => IsFinite(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsFiniteUInt16Test(ushort value) => IsFinite(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsFiniteUInt32Test(uint value) => IsFinite(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsFiniteUInt64Test(ulong value) => IsFinite(value);

        private void IsInfinity<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsInfinity(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsInfinity(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsInfinityByteTest(byte value) => IsInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsInfinityDoubleTest(double value) => IsInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsInfinityInt16Test(short value) => IsInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsInfinityInt32Test(int value) => IsInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsInfinityInt64Test(long value) => IsInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsInfinitySByteTest(sbyte value) => IsInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsInfinitySingleTest(float value) => IsInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsInfinityUInt16Test(ushort value) => IsInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsInfinityUInt32Test(uint value) => IsInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsInfinityUInt64Test(ulong value) => IsInfinity(value);

        private void IsInteger<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsInteger(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsInteger(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsIntegerByteTest(byte value) => IsInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsIntegerDoubleTest(double value) => IsInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsIntegerInt16Test(short value) => IsInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsIntegerInt32Test(int value) => IsInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsIntegerInt64Test(long value) => IsInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsIntegerSByteTest(sbyte value) => IsInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsIntegerSingleTest(float value) => IsInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsIntegerUInt16Test(ushort value) => IsInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsIntegerUInt32Test(uint value) => IsInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsIntegerUInt64Test(ulong value) => IsInteger(value);

        private void IsNaN<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsNaN(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsNaN(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNaNByteTest(byte value) => IsNaN(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNaNDoubleTest(double value) => IsNaN(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNaNInt16Test(short value) => IsNaN(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNaNInt32Test(int value) => IsNaN(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNaNInt64Test(long value) => IsNaN(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNaNSByteTest(sbyte value) => IsNaN(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNaNSingleTest(float value) => IsNaN(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNaNUInt16Test(ushort value) => IsNaN(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNaNUInt32Test(uint value) => IsNaN(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNaNUInt64Test(ulong value) => IsNaN(value);

        private void IsNegative<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsNegative(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsNegative(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeByteTest(byte value) => IsNegative(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeDoubleTest(double value) => IsNegative(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInt16Test(short value) => IsNegative(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInt32Test(int value) => IsNegative(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInt64Test(long value) => IsNegative(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeSByteTest(sbyte value) => IsNegative(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeSingleTest(float value) => IsNegative(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeUInt16Test(ushort value) => IsNegative(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeUInt32Test(uint value) => IsNegative(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeUInt64Test(ulong value) => IsNegative(value);

        private void IsNegativeInfinity<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsNegativeInfinity(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsNegativeInfinity(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInfinityByteTest(byte value) => IsNegativeInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInfinityDoubleTest(double value) => IsNegativeInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInfinityInt16Test(short value) => IsNegativeInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInfinityInt32Test(int value) => IsNegativeInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInfinityInt64Test(long value) => IsNegativeInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInfinitySByteTest(sbyte value) => IsNegativeInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInfinitySingleTest(float value) => IsNegativeInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInfinityUInt16Test(ushort value) => IsNegativeInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInfinityUInt32Test(uint value) => IsNegativeInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNegativeInfinityUInt64Test(ulong value) => IsNegativeInfinity(value);

        private void IsNormal<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsNormal(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsNormal(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNormalByteTest(byte value) => IsNormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNormalDoubleTest(double value) => IsNormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNormalInt16Test(short value) => IsNormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNormalInt32Test(int value) => IsNormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNormalInt64Test(long value) => IsNormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNormalSByteTest(sbyte value) => IsNormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNormalSingleTest(float value) => IsNormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNormalUInt16Test(ushort value) => IsNormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNormalUInt32Test(uint value) => IsNormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsNormalUInt64Test(ulong value) => IsNormal(value);

        private void IsOddInteger<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsOddInteger(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsOddInteger(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsOddIntegerByteTest(byte value) => IsOddInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsOddIntegerDoubleTest(double value) => IsOddInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsOddIntegerInt16Test(short value) => IsOddInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsOddIntegerInt32Test(int value) => IsOddInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsOddIntegerInt64Test(long value) => IsOddInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsOddIntegerSByteTest(sbyte value) => IsOddInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsOddIntegerSingleTest(float value) => IsOddInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsOddIntegerUInt16Test(ushort value) => IsOddInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsOddIntegerUInt32Test(uint value) => IsOddInteger(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsOddIntegerUInt64Test(ulong value) => IsOddInteger(value);

        private void IsPositive<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsPositive(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsPositive(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveByteTest(byte value) => IsPositive(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveDoubleTest(double value) => IsPositive(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInt16Test(short value) => IsPositive(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInt32Test(int value) => IsPositive(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInt64Test(long value) => IsPositive(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveSByteTest(sbyte value) => IsPositive(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveSingleTest(float value) => IsPositive(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveUInt16Test(ushort value) => IsPositive(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveUInt32Test(uint value) => IsPositive(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveUInt64Test(ulong value) => IsPositive(value);

        private void IsPositiveInfinity<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsPositiveInfinity(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsPositiveInfinity(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInfinityByteTest(byte value) => IsPositiveInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInfinityDoubleTest(double value) => IsPositiveInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInfinityInt16Test(short value) => IsPositiveInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInfinityInt32Test(int value) => IsPositiveInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInfinityInt64Test(long value) => IsPositiveInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInfinitySByteTest(sbyte value) => IsPositiveInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInfinitySingleTest(float value) => IsPositiveInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInfinityUInt16Test(ushort value) => IsPositiveInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInfinityUInt32Test(uint value) => IsPositiveInfinity(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsPositiveInfinityUInt64Test(ulong value) => IsPositiveInfinity(value);

        private void IsSubnormal<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsSubnormal(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsSubnormal(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsSubnormalByteTest(byte value) => IsSubnormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsSubnormalDoubleTest(double value) => IsSubnormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsSubnormalInt16Test(short value) => IsSubnormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsSubnormalInt32Test(int value) => IsSubnormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsSubnormalInt64Test(long value) => IsSubnormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsSubnormalSByteTest(sbyte value) => IsSubnormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsSubnormalSingleTest(float value) => IsSubnormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsSubnormalUInt16Test(ushort value) => IsSubnormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsSubnormalUInt32Test(uint value) => IsSubnormal(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsSubnormalUInt64Test(ulong value) => IsSubnormal(value);

        private void IsZero<T>(T value)
            where T : INumber<T>
        {
            Assert.Equal(T.IsZero(value) ? Vector512<T>.AllBitsSet : Vector512<T>.Zero, Vector512.IsZero(Vector512.Create(value)));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsZeroByteTest(byte value) => IsZero(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void IsZeroDoubleTest(double value) => IsZero(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsZeroInt16Test(short value) => IsZero(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsZeroInt32Test(int value) => IsZero(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsZeroInt64Test(long value) => IsZero(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSByte), MemberType = typeof(GenericMathTestMemberData))]
        public void IsZeroSByteTest(sbyte value) => IsZero(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void IsZeroSingleTest(float value) => IsZero(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt16), MemberType = typeof(GenericMathTestMemberData))]
        public void IsZeroUInt16Test(ushort value) => IsZero(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt32), MemberType = typeof(GenericMathTestMemberData))]
        public void IsZeroUInt32Test(uint value) => IsZero(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.IsTestUInt64), MemberType = typeof(GenericMathTestMemberData))]
        public void IsZeroUInt64Test(ulong value) => IsZero(value);

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.LerpDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void LerpDoubleTest(double x, double y, double amount, double expectedResult)
        {
            AssertEqual(Vector512.Create(+expectedResult), Vector512.Lerp(Vector512.Create(+x), Vector512.Create(+y), Vector512.Create(amount)), Vector512<double>.Zero);
            AssertEqual(Vector512.Create((expectedResult == 0.0) ? expectedResult : -expectedResult), Vector512.Lerp(Vector512.Create(-x), Vector512.Create(-y), Vector512.Create(amount)), Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.LerpSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void LerpSingleTest(float x, float y, float amount, float expectedResult)
        {
            AssertEqual(Vector512.Create(+expectedResult), Vector512.Lerp(Vector512.Create(+x), Vector512.Create(+y), Vector512.Create(amount)), Vector512<float>.Zero);
            AssertEqual(Vector512.Create((expectedResult == 0.0f) ? expectedResult : -expectedResult), Vector512.Lerp(Vector512.Create(-x), Vector512.Create(-y), Vector512.Create(amount)), Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MaxDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void MaxDoubleTest(double x, double y, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.Max(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MaxSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void MaxSingleTest(float x, float y, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.Max(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MaxMagnitudeDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void MaxMagnitudeDoubleTest(double x, double y, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.MaxMagnitude(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MaxMagnitudeSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void MaxMagnitudeSingleTest(float x, float y, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.MaxMagnitude(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MaxMagnitudeNumberDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void MaxMagnitudeNumberDoubleTest(double x, double y, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.MaxMagnitudeNumber(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MaxMagnitudeNumberSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void MaxMagnitudeNumberSingleTest(float x, float y, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.MaxMagnitudeNumber(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MaxNumberDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void MaxNumberDoubleTest(double x, double y, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.MaxNumber(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MaxNumberSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void MaxNumberSingleTest(float x, float y, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.MaxNumber(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MinDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void MinDoubleTest(double x, double y, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.Min(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MinSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void MinSingleTest(float x, float y, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.Min(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MinMagnitudeDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void MinMagnitudeDoubleTest(double x, double y, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.MinMagnitude(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MinMagnitudeSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void MinMagnitudeSingleTest(float x, float y, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.MinMagnitude(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MinMagnitudeNumberDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void MinMagnitudeNumberDoubleTest(double x, double y, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.MinMagnitudeNumber(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MinMagnitudeNumberSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void MinMagnitudeNumberSingleTest(float x, float y, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.MinMagnitudeNumber(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MinNumberDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void MinNumberDoubleTest(double x, double y, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.MinNumber(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.MinNumberSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void MinNumberSingleTest(float x, float y, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.MinNumber(Vector512.Create(x), Vector512.Create(y));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.RadiansToDegreesDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void RadiansToDegreesDoubleTest(double value, double expectedResult, double variance)
        {
            AssertEqual(Vector512.Create(-expectedResult), Vector512.RadiansToDegrees(Vector512.Create(-value)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(+expectedResult), Vector512.RadiansToDegrees(Vector512.Create(+value)), Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.RadiansToDegreesSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void RadiansToDegreesSingleTest(float value, float expectedResult, float variance)
        {
            AssertEqual(Vector512.Create(-expectedResult), Vector512.RadiansToDegrees(Vector512.Create(-value)), Vector512.Create(variance));
            AssertEqual(Vector512.Create(+expectedResult), Vector512.RadiansToDegrees(Vector512.Create(+value)), Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.RoundDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void RoundDoubleTest(double value, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.Round(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.RoundSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void RoundSingleTest(float value, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.Round(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.RoundAwayFromZeroDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void RoundAwayFromZeroDoubleTest(double value, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.Round(Vector512.Create(value), MidpointRounding.AwayFromZero);
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.RoundAwayFromZeroSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void RoundAwayFromZeroSingleTest(float value, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.Round(Vector512.Create(value), MidpointRounding.AwayFromZero);
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.RoundToEvenDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void RoundToEvenDoubleTest(double value, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.Round(Vector512.Create(value), MidpointRounding.ToEven);
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.RoundToEvenSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void RoundToEvenSingleTest(float value, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.Round(Vector512.Create(value), MidpointRounding.ToEven);
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.SinDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void SinDoubleTest(double value, double expectedResult, double variance)
        {
            Vector512<double> actualResult = Vector512.Sin(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.SinSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void SinSingleTest(float value, float expectedResult, float variance)
        {
            Vector512<float> actualResult = Vector512.Sin(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.SinCosDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void SinCosDoubleTest(double value, double expectedResultSin, double expectedResultCos, double allowedVarianceSin, double allowedVarianceCos)
        {
            (Vector512<double> resultSin, Vector512<double> resultCos) = Vector512.SinCos(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResultSin), resultSin, Vector512.Create(allowedVarianceSin));
            AssertEqual(Vector512.Create(expectedResultCos), resultCos, Vector512.Create(allowedVarianceCos));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.SinCosSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void SinCosSingleTest(float value, float expectedResultSin, float expectedResultCos, float allowedVarianceSin, float allowedVarianceCos)
        {
            (Vector512<float> resultSin, Vector512<float> resultCos) = Vector512.SinCos(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResultSin), resultSin, Vector512.Create(allowedVarianceSin));
            AssertEqual(Vector512.Create(expectedResultCos), resultCos, Vector512.Create(allowedVarianceCos));
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.TruncateDouble), MemberType = typeof(GenericMathTestMemberData))]
        public void TruncateDoubleTest(double value, double expectedResult)
        {
            Vector512<double> actualResult = Vector512.Truncate(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<double>.Zero);
        }

        [Theory]
        [MemberData(nameof(GenericMathTestMemberData.TruncateSingle), MemberType = typeof(GenericMathTestMemberData))]
        public void TruncateSingleTest(float value, float expectedResult)
        {
            Vector512<float> actualResult = Vector512.Truncate(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512<float>.Zero);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AllAnyNoneTest<T>(T value1, T value2)
            where T : struct, INumber<T>
        {
            var input1 = Vector512.Create<T>(value1);
            var input2 = Vector512.Create<T>(value2);

            Assert.True(Vector512.All(input1, value1));
            Assert.True(Vector512.All(input2, value2));
            Assert.False(Vector512.All(input1.WithElement(0, value2), value1));
            Assert.False(Vector512.All(input2.WithElement(0, value1), value2));
            Assert.False(Vector512.All(input1, value2));
            Assert.False(Vector512.All(input2, value1));
            Assert.False(Vector512.All(input1.WithElement(0, value2), value2));
            Assert.False(Vector512.All(input2.WithElement(0, value1), value1));

            Assert.True(Vector512.Any(input1, value1));
            Assert.True(Vector512.Any(input2, value2));
            Assert.True(Vector512.Any(input1.WithElement(0, value2), value1));
            Assert.True(Vector512.Any(input2.WithElement(0, value1), value2));
            Assert.False(Vector512.Any(input1, value2));
            Assert.False(Vector512.Any(input2, value1));
            Assert.True(Vector512.Any(input1.WithElement(0, value2), value2));
            Assert.True(Vector512.Any(input2.WithElement(0, value1), value1));

            Assert.False(Vector512.None(input1, value1));
            Assert.False(Vector512.None(input2, value2));
            Assert.False(Vector512.None(input1.WithElement(0, value2), value1));
            Assert.False(Vector512.None(input2.WithElement(0, value1), value2));
            Assert.True(Vector512.None(input1, value2));
            Assert.True(Vector512.None(input2, value1));
            Assert.False(Vector512.None(input1.WithElement(0, value2), value2));
            Assert.False(Vector512.None(input2.WithElement(0, value1), value1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AllAnyNoneTest_IFloatingPointIeee754<T>(T value)
            where T : struct, IFloatingPointIeee754<T>
        {
            var input = Vector512.Create<T>(value);

            Assert.False(Vector512.All(input, value));
            Assert.False(Vector512.Any(input, value));
            Assert.True(Vector512.None(input, value));
        }

        [Fact]
        public void AllAnyNoneByteTest() => AllAnyNoneTest<byte>(3, 2);

        [Fact]
        public void AllAnyNoneDoubleTest() => AllAnyNoneTest<double>(3, 2);

        [Fact]
        public void AllAnyNoneDoubleTest_AllBitsSet() => AllAnyNoneTest_IFloatingPointIeee754<double>(BitConverter.Int64BitsToDouble(-1));

        [Fact]
        public void AllAnyNoneInt16Test() => AllAnyNoneTest<short>(3, 2);

        [Fact]
        public void AllAnyNoneInt32Test() => AllAnyNoneTest<int>(3, 2);

        [Fact]
        public void AllAnyNoneInt64Test() => AllAnyNoneTest<long>(3, 2);

        [Fact]
        public void AllAnyNoneSByteTest() => AllAnyNoneTest<sbyte>(3, 2);

        [Fact]
        public void AllAnyNoneSingleTest() => AllAnyNoneTest<float>(3, 2);

        [Fact]
        public void AllAnyNoneSingleTest_AllBitsSet() => AllAnyNoneTest_IFloatingPointIeee754<float>(BitConverter.Int32BitsToSingle(-1));

        [Fact]
        public void AllAnyNoneUInt16Test() => AllAnyNoneTest<ushort>(3, 2);

        [Fact]
        public void AllAnyNoneUInt32Test() => AllAnyNoneTest<uint>(3, 2);

        [Fact]
        public void AllAnyNoneUInt64Test() => AllAnyNoneTest<ulong>(3, 2);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AllAnyNoneWhereAllBitsSetTest<T>(T allBitsSet, T value2)
            where T : struct, INumber<T>
        {
            var input1 = Vector512.Create<T>(allBitsSet);
            var input2 = Vector512.Create<T>(value2);

            Assert.True(Vector512.AllWhereAllBitsSet(input1));
            Assert.False(Vector512.AllWhereAllBitsSet(input2));
            Assert.False(Vector512.AllWhereAllBitsSet(input1.WithElement(0, value2)));
            Assert.False(Vector512.AllWhereAllBitsSet(input2.WithElement(0, allBitsSet)));

            Assert.True(Vector512.AnyWhereAllBitsSet(input1));
            Assert.False(Vector512.AnyWhereAllBitsSet(input2));
            Assert.True(Vector512.AnyWhereAllBitsSet(input1.WithElement(0, value2)));
            Assert.True(Vector512.AnyWhereAllBitsSet(input2.WithElement(0, allBitsSet)));

            Assert.False(Vector512.NoneWhereAllBitsSet(input1));
            Assert.True(Vector512.NoneWhereAllBitsSet(input2));
            Assert.False(Vector512.NoneWhereAllBitsSet(input1.WithElement(0, value2)));
            Assert.False(Vector512.NoneWhereAllBitsSet(input2.WithElement(0, allBitsSet)));
        }

        [Fact]
        public void AllAnyNoneWhereAllBitsSetByteTest() => AllAnyNoneWhereAllBitsSetTest<byte>(byte.MaxValue, 2);

        [Fact]
        public void AllAnyNoneWhereAllBitsSetDoubleTest() => AllAnyNoneWhereAllBitsSetTest<double>(BitConverter.Int64BitsToDouble(-1), 2);

        [Fact]
        public void AllAnyNoneWhereAllBitsSetInt16Test() => AllAnyNoneWhereAllBitsSetTest<short>(-1, 2);

        [Fact]
        public void AllAnyNoneWhereAllBitsSetInt32Test() => AllAnyNoneWhereAllBitsSetTest<int>(-1, 2);

        [Fact]
        public void AllAnyNoneWhereAllBitsSetInt64Test() => AllAnyNoneWhereAllBitsSetTest<long>(-1, 2);

        [Fact]
        public void AllAnyNoneWhereAllBitsSetSByteTest() => AllAnyNoneWhereAllBitsSetTest<sbyte>(-1, 2);

        [Fact]
        public void AllAnyNoneWhereAllBitsSetSingleTest() => AllAnyNoneWhereAllBitsSetTest<float>(BitConverter.Int32BitsToSingle(-1), 2);

        [Fact]
        public void AllAnyNoneWhereAllBitsSetUInt16Test() => AllAnyNoneWhereAllBitsSetTest<ushort>(ushort.MaxValue, 2);

        [Fact]
        public void AllAnyNoneWhereAllBitsSetUInt32Test() => AllAnyNoneWhereAllBitsSetTest<uint>(uint.MaxValue, 2);

        [Fact]
        public void AllAnyNoneWhereAllBitsSetUInt64Test() => AllAnyNoneWhereAllBitsSetTest<ulong>(ulong.MaxValue, 2);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CountIndexOfLastIndexOfTest<T>(T value1, T value2)
            where T : struct, INumber<T>
        {
            var input1 = Vector512.Create<T>(value1);
            var input2 = Vector512.Create<T>(value2);

            Assert.Equal(Vector512<T>.Count, Vector512.Count(input1, value1));
            Assert.Equal(Vector512<T>.Count, Vector512.Count(input2, value2));
            Assert.Equal(Vector512<T>.Count - 1, Vector512.Count(input1.WithElement(0, value2), value1));
            Assert.Equal(Vector512<T>.Count - 1, Vector512.Count(input2.WithElement(0, value1), value2));
            Assert.Equal(0, Vector512.Count(input1, value2));
            Assert.Equal(0, Vector512.Count(input2, value1));
            Assert.Equal(1, Vector512.Count(input1.WithElement(0, value2), value2));
            Assert.Equal(1, Vector512.Count(input2.WithElement(0, value1), value1));

            Assert.Equal(0, Vector512.IndexOf(input1, value1));
            Assert.Equal(0, Vector512.IndexOf(input2, value2));
            Assert.Equal(1, Vector512.IndexOf(input1.WithElement(0, value2), value1));
            Assert.Equal(1, Vector512.IndexOf(input2.WithElement(0, value1), value2));
            Assert.Equal(-1, Vector512.IndexOf(input1, value2));
            Assert.Equal(-1, Vector512.IndexOf(input2, value1));
            Assert.Equal(0, Vector512.IndexOf(input1.WithElement(0, value2), value2));
            Assert.Equal(0, Vector512.IndexOf(input2.WithElement(0, value1), value1));

            Assert.Equal(Vector512<T>.Count - 1, Vector512.LastIndexOf(input1, value1));
            Assert.Equal(Vector512<T>.Count - 1, Vector512.LastIndexOf(input2, value2));
            Assert.Equal(Vector512<T>.Count - 1, Vector512.LastIndexOf(input1.WithElement(0, value2), value1));
            Assert.Equal(Vector512<T>.Count - 1, Vector512.LastIndexOf(input2.WithElement(0, value1), value2));
            Assert.Equal(-1, Vector512.LastIndexOf(input1, value2));
            Assert.Equal(-1, Vector512.LastIndexOf(input2, value1));
            Assert.Equal(0, Vector512.LastIndexOf(input1.WithElement(0, value2), value2));
            Assert.Equal(0, Vector512.LastIndexOf(input2.WithElement(0, value1), value1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CountIndexOfLastIndexOfTest_IFloatingPointIeee754<T>(T value)
            where T : struct, IFloatingPointIeee754<T>
        {
            var input = Vector512.Create<T>(value);

            Assert.Equal(0, Vector512.Count(input, value));
            Assert.Equal(-1, Vector512.IndexOf(input, value));
            Assert.Equal(-1, Vector512.LastIndexOf(input, value));
        }

        [Fact]
        public void CountIndexOfLastIndexOfByteTest() => CountIndexOfLastIndexOfTest<byte>(3, 2);

        [Fact]
        public void CountIndexOfLastIndexOfDoubleTest() => CountIndexOfLastIndexOfTest<double>(3, 2);

        [Fact]
        public void CountIndexOfLastIndexOfDoubleTest_AllBitsSet() => CountIndexOfLastIndexOfTest_IFloatingPointIeee754<double>(BitConverter.Int64BitsToDouble(-1));

        [Fact]
        public void CountIndexOfLastIndexOfInt16Test() => CountIndexOfLastIndexOfTest<short>(3, 2);

        [Fact]
        public void CountIndexOfLastIndexOfInt32Test() => CountIndexOfLastIndexOfTest<int>(3, 2);

        [Fact]
        public void CountIndexOfLastIndexOfInt64Test() => CountIndexOfLastIndexOfTest<long>(3, 2);

        [Fact]
        public void CountIndexOfLastIndexOfSByteTest() => CountIndexOfLastIndexOfTest<sbyte>(3, 2);

        [Fact]
        public void CountIndexOfLastIndexOfSingleTest() => CountIndexOfLastIndexOfTest<float>(3, 2);

        [Fact]
        public void CountIndexOfLastIndexOfSingleTest_AllBitsSet() => CountIndexOfLastIndexOfTest_IFloatingPointIeee754<float>(BitConverter.Int32BitsToSingle(-1));

        [Fact]
        public void CountIndexOfLastIndexOfUInt16Test() => CountIndexOfLastIndexOfTest<ushort>(3, 2);

        [Fact]
        public void CountIndexOfLastIndexOfUInt32Test() => CountIndexOfLastIndexOfTest<uint>(3, 2);

        [Fact]
        public void CountIndexOfLastIndexOfUInt64Test() => CountIndexOfLastIndexOfTest<ulong>(3, 2);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CountIndexOfLastIndexOfWhereAllBitsSetTest<T>(T allBitsSet, T value2)
            where T : struct, INumber<T>
        {
            var input1 = Vector512.Create<T>(allBitsSet);
            var input2 = Vector512.Create<T>(value2);

            Assert.Equal(Vector512<T>.Count, Vector512.CountWhereAllBitsSet(input1));
            Assert.Equal(0, Vector512.CountWhereAllBitsSet(input2));
            Assert.Equal(Vector512<T>.Count - 1, Vector512.CountWhereAllBitsSet(input1.WithElement(0, value2)));
            Assert.Equal(1, Vector512.CountWhereAllBitsSet(input2.WithElement(0, allBitsSet)));

            Assert.Equal(0, Vector512.IndexOfWhereAllBitsSet(input1));
            Assert.Equal(-1, Vector512.IndexOfWhereAllBitsSet(input2));
            Assert.Equal(1, Vector512.IndexOfWhereAllBitsSet(input1.WithElement(0, value2)));
            Assert.Equal(0, Vector512.IndexOfWhereAllBitsSet(input2.WithElement(0, allBitsSet)));

            Assert.Equal(Vector512<T>.Count - 1, Vector512.LastIndexOfWhereAllBitsSet(input1));
            Assert.Equal(-1, Vector512.LastIndexOfWhereAllBitsSet(input2));
            Assert.Equal(Vector512<T>.Count - 1, Vector512.LastIndexOfWhereAllBitsSet(input1.WithElement(0, value2)));
            Assert.Equal(0, Vector512.LastIndexOfWhereAllBitsSet(input2.WithElement(0, allBitsSet)));
        }

        [Fact]
        public void CountIndexOfLastIndexOfWhereAllBitsSetByteTest() => CountIndexOfLastIndexOfWhereAllBitsSetTest<byte>(byte.MaxValue, 2);

        [Fact]
        public void CountIndexOfLastIndexOfWhereAllBitsSetDoubleTest() => CountIndexOfLastIndexOfWhereAllBitsSetTest<double>(BitConverter.Int64BitsToDouble(-1), 2);

        [Fact]
        public void CountIndexOfLastIndexOfWhereAllBitsSetInt16Test() => CountIndexOfLastIndexOfWhereAllBitsSetTest<short>(-1, 2);

        [Fact]
        public void CountIndexOfLastIndexOfWhereAllBitsSetInt32Test() => CountIndexOfLastIndexOfWhereAllBitsSetTest<int>(-1, 2);

        [Fact]
        public void CountIndexOfLastIndexOfWhereAllBitsSetInt64Test() => CountIndexOfLastIndexOfWhereAllBitsSetTest<long>(-1, 2);

        [Fact]
        public void CountIndexOfLastIndexOfWhereAllBitsSetSByteTest() => CountIndexOfLastIndexOfWhereAllBitsSetTest<sbyte>(-1, 2);

        [Fact]
        public void CountIndexOfLastIndexOfWhereAllBitsSetSingleTest() => CountIndexOfLastIndexOfWhereAllBitsSetTest<float>(BitConverter.Int32BitsToSingle(-1), 2);

        [Fact]
        public void CountIndexOfLastIndexOfWhereAllBitsSetUInt16Test() => CountIndexOfLastIndexOfWhereAllBitsSetTest<ushort>(ushort.MaxValue, 2);

        [Fact]
        public void CountIndexOfLastIndexOfWhereAllBitsSetUInt32Test() => CountIndexOfLastIndexOfWhereAllBitsSetTest<uint>(uint.MaxValue, 2);

        [Fact]
        public void CountIndexOfLastIndexOfWhereAllBitsSetUInt64Test() => CountIndexOfLastIndexOfWhereAllBitsSetTest<ulong>(ulong.MaxValue, 2);
    }
}
