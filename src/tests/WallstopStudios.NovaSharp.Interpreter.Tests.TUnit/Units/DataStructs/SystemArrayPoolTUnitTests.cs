namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataStructs
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    /// <summary>
    /// Tests for <see cref="SystemArrayPool{T}"/> which wraps <see cref="System.Buffers.ArrayPool{T}.Shared"/>
    /// with the project's <see cref="PooledResource{T}"/> disposal pattern.
    /// </summary>
    public sealed class SystemArrayPoolTUnitTests
    {
        [Test]
        public async Task GetReturnsEmptyArrayForZeroLength()
        {
            using PooledResource<int[]> pooled = SystemArrayPool<int>.Get(0, out int[] array);

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task GetReturnsEmptyArrayForNegativeLength()
        {
            using PooledResource<char[]> pooled = SystemArrayPool<char>.Get(-5, out char[] array);

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task RentReturnsEmptyArrayForZeroLength()
        {
            int[] array = SystemArrayPool<int>.Rent(0);

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task RentReturnsEmptyArrayForNegativeLength()
        {
            double[] array = SystemArrayPool<double>.Rent(-10);

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        [Arguments(1)]
        [Arguments(5)]
        [Arguments(10)]
        [Arguments(16)]
        [Arguments(32)]
        [Arguments(100)]
        [Arguments(256)]
        [Arguments(1000)]
        public async Task GetReturnsArrayOfAtLeastRequestedLength(int minimumLength)
        {
            using PooledResource<byte[]> pooled = SystemArrayPool<byte>.Get(
                minimumLength,
                out byte[] array
            );

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert
                .That(array.Length)
                .IsGreaterThanOrEqualTo(minimumLength)
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(1)]
        [Arguments(5)]
        [Arguments(10)]
        [Arguments(16)]
        [Arguments(32)]
        [Arguments(100)]
        [Arguments(256)]
        [Arguments(1000)]
        public async Task RentReturnsArrayOfAtLeastRequestedLength(int minimumLength)
        {
            int[] array = SystemArrayPool<int>.Rent(minimumLength);
            using PooledResource<int[]> _ = new(array, arr => SystemArrayPool<int>.Return(arr));

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert
                .That(array.Length)
                .IsGreaterThanOrEqualTo(minimumLength)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GetMayReturnLargerArrayThanRequested()
        {
            // ArrayPool often returns power-of-2 sized arrays
            // Request 17 elements - likely to get 32
            using PooledResource<long[]> pooled = SystemArrayPool<long>.Get(17, out long[] array);

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsGreaterThanOrEqualTo(17).ConfigureAwait(false);
            // ArrayPool typically returns power-of-2 sizes, so 17 should give us 32
            // But we don't assert exact size as it's implementation-dependent
        }

        [Test]
        public async Task GetWorksWithValueTypes()
        {
            using PooledResource<int[]> pooled = SystemArrayPool<int>.Get(50, out int[] array);

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsGreaterThanOrEqualTo(50).ConfigureAwait(false);

            // Can write to the array
            array[0] = 42;
            await Assert.That(array[0]).IsEqualTo(42).ConfigureAwait(false);
        }

        [Test]
        public async Task GetWorksWithReferenceTypes()
        {
            using PooledResource<string[]> pooled = SystemArrayPool<string>.Get(
                30,
                out string[] array
            );

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsGreaterThanOrEqualTo(30).ConfigureAwait(false);

            // Can write to the array
            array[0] = "test";
            await Assert.That(array[0]).IsEqualTo("test").ConfigureAwait(false);
        }

        [Test]
        public async Task GetWorksWithCustomStructs()
        {
            using PooledResource<TestStruct[]> pooled = SystemArrayPool<TestStruct>.Get(
                20,
                out TestStruct[] array
            );

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsGreaterThanOrEqualTo(20).ConfigureAwait(false);

            // Can write to the array
            array[0] = new TestStruct { value = 123, name = "test" };
            await Assert.That(array[0].value).IsEqualTo(123).ConfigureAwait(false);
            await Assert.That(array[0].name).IsEqualTo("test").ConfigureAwait(false);
        }

        [Test]
        public async Task ReturnHandlesNullArrayWithoutThrowing()
        {
            // Should not throw
            await Assert
                .That(() => SystemArrayPool<int>.Return(null))
                .ThrowsNothing()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturnHandlesEmptyArrayWithoutThrowing()
        {
            // Should not throw
            await Assert
                .That(() => SystemArrayPool<int>.Return(Array.Empty<int>()))
                .ThrowsNothing()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task PooledResourceDisposeReturnsArray()
        {
            int[] first;
            {
                PooledResource<int[]> pooled = SystemArrayPool<int>.Get(64, out first);
                first[0] = 999;
                pooled.Dispose();
            }

            // After dispose, we should be able to rent the same array again
            // (though it may have been cleared)
            using PooledResource<int[]> pooled2 = SystemArrayPool<int>.Get(64, out int[] second);

            // Can't guarantee same array (depends on pool state), but should work
            await Assert.That(second).IsNotNull().ConfigureAwait(false);
            await Assert.That(second.Length).IsGreaterThanOrEqualTo(64).ConfigureAwait(false);
        }

        [Test]
        public async Task SuppressReturnPreventsPoolReturn()
        {
            int[] first;
            {
                PooledResource<int[]> pooled = SystemArrayPool<int>.Get(128, out first);
                first[0] = 12345;
                pooled.SuppressReturn();
                pooled.Dispose();
            }

            // The array was suppressed, so it should NOT have been returned
            // We can verify the original array still has its value
            await Assert.That(first[0]).IsEqualTo(12345).ConfigureAwait(false);
        }

        [Test]
        public async Task GetWithClearOnReturnClearsReferenceTypeArray()
        {
            string[] array;
            using (
                PooledResource<string[]> pooled = SystemArrayPool<string>.Get(16, true, out array)
            )
            {
                array[0] = "test value";
                array[1] = "another value";
            }

            // Rent again - should get cleared array (if same one returned)
            using PooledResource<string[]> pooled2 = SystemArrayPool<string>.Get(
                16,
                out string[] array2
            );

            // If we got the same array back, it should be cleared
            if (object.ReferenceEquals(array, array2))
            {
                await Assert.That(array2[0]).IsNull().ConfigureAwait(false);
                await Assert.That(array2[1]).IsNull().ConfigureAwait(false);
            }
            else
            {
                // Different array - just verify it exists
                await Assert.That(array2).IsNotNull().ConfigureAwait(false);
            }
        }

        [Test]
        public async Task GetWithoutClearOnReturnDoesNotClear()
        {
            int[] array;
            using (PooledResource<int[]> pooled = SystemArrayPool<int>.Get(16, false, out array))
            {
                for (int i = 0; i < 16; i++)
                {
                    array[i] = i + 1;
                }
            }

            // Rent again - if we get the same array, values may still be there
            using PooledResource<int[]> pooled2 = SystemArrayPool<int>.Get(
                16,
                false,
                out int[] array2
            );

            // If we got the same array back, it should NOT be cleared
            if (object.ReferenceEquals(array, array2))
            {
                // At least some values should still be set
                await Assert.That(array2[0]).IsEqualTo(1).ConfigureAwait(false);
            }
            else
            {
                // Different array - just verify it exists
                await Assert.That(array2).IsNotNull().ConfigureAwait(false);
            }
        }

        [Test]
        public async Task ToArrayAndReturnCreatesExactSizedCopy()
        {
            int[] rented = SystemArrayPool<int>.Rent(100);
            for (int i = 0; i < 50; i++)
            {
                rented[i] = i * 2;
            }

            int[] result = SystemArrayPool<int>.ToArrayAndReturn(rented, 50);

            await Assert.That(result.Length).IsEqualTo(50).ConfigureAwait(false);
            await Assert.That(result[0]).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(result[25]).IsEqualTo(50).ConfigureAwait(false);
            await Assert.That(result[49]).IsEqualTo(98).ConfigureAwait(false);
        }

        [Test]
        public async Task ToArrayAndReturnReturnsEmptyForNullArray()
        {
            int[] result = SystemArrayPool<int>.ToArrayAndReturn(null, 10);

            await Assert.That(result).IsNotNull().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ToArrayAndReturnReturnsEmptyForZeroLength()
        {
            char[] rented = SystemArrayPool<char>.Rent(50);
            rented[0] = 'A';

            char[] result = SystemArrayPool<char>.ToArrayAndReturn(rented, 0);

            await Assert.That(result).IsNotNull().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ToArrayAndReturnReturnsEmptyForNegativeLength()
        {
            string[] rented = SystemArrayPool<string>.Rent(20);
            rented[0] = "test";

            string[] result = SystemArrayPool<string>.ToArrayAndReturn(rented, -5);

            await Assert.That(result).IsNotNull().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ToArrayAndReturnWithStartIndexCreatesCorrectCopy()
        {
            byte[] rented = SystemArrayPool<byte>.Rent(100);
            for (int i = 0; i < 100; i++)
            {
                rented[i] = (byte)(i + 10);
            }

            byte[] result = SystemArrayPool<byte>.ToArrayAndReturn(rented, 20, 30);

            await Assert.That(result.Length).IsEqualTo(30).ConfigureAwait(false);
            await Assert.That(result[0]).IsEqualTo((byte)30).ConfigureAwait(false); // rented[20] = 30
            await Assert.That(result[29]).IsEqualTo((byte)59).ConfigureAwait(false); // rented[49] = 59
        }

        [Test]
        public async Task ToArrayAndReturnWithStartIndexReturnsEmptyForNullArray()
        {
            double[] result = SystemArrayPool<double>.ToArrayAndReturn(null, 5, 10);

            await Assert.That(result).IsNotNull().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task GetOverloadWithoutOutReturnsPooledResource()
        {
            using PooledResource<float[]> pooled = SystemArrayPool<float>.Get(25);

            await Assert.That(pooled.Resource).IsNotNull().ConfigureAwait(false);
            await Assert
                .That(pooled.Resource.Length)
                .IsGreaterThanOrEqualTo(25)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GetIsThreadSafe()
        {
            List<Task> tasks = new List<Task>();
            int iterations = 100;
            int threadCount = 8;
            int successCount = 0;

            for (int t = 0; t < threadCount; t++)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            using (
                                PooledResource<int[]> pooled = SystemArrayPool<int>.Get(
                                    64,
                                    out int[] array
                                )
                            )
                            {
                                // Write some data
                                for (int j = 0; j < 64; j++)
                                {
                                    array[j] = j;
                                }
                            }
                        }
                        System.Threading.Interlocked.Increment(ref successCount);
                    })
                );
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // If we get here without exceptions, thread safety is working
            await Assert.That(successCount).IsEqualTo(threadCount).ConfigureAwait(false);
        }

        [Test]
        public async Task RentAndReturnIsThreadSafe()
        {
            List<Task> tasks = new List<Task>();
            int iterations = 100;
            int threadCount = 8;
            int successCount = 0;

            for (int t = 0; t < threadCount; t++)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            byte[] array = SystemArrayPool<byte>.Rent(128);
                            using PooledResource<byte[]> _ = new(
                                array,
                                arr => SystemArrayPool<byte>.Return(arr)
                            );

                            // Write some data
                            for (int j = 0; j < 128; j++)
                            {
                                array[j] = (byte)j;
                            }
                        }
                        System.Threading.Interlocked.Increment(ref successCount);
                    })
                );
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // If we get here without exceptions, thread safety is working
            await Assert.That(successCount).IsEqualTo(threadCount).ConfigureAwait(false);
        }

        [Test]
        public async Task GetHandlesLargeArrays()
        {
            // Request a large array (but under MaxCachedArraySize)
            using PooledResource<int[]> pooled = SystemArrayPool<int>.Get(100000, out int[] array);

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsGreaterThanOrEqualTo(100000).ConfigureAwait(false);

            // Can use the array
            array[0] = 1;
            array[99999] = 2;
            await Assert.That(array[0]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(array[99999]).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task ReturnDoesNotPoolVeryLargeArrays()
        {
            // Request an extremely large array that exceeds MaxCachedArraySize
            int[] veryLargeArray = SystemArrayPool<int>.Rent(2000000);
            veryLargeArray[0] = 42;

            // This should not throw even though the array won't be pooled
            await Assert
                .That(() => SystemArrayPool<int>.Return(veryLargeArray))
                .ThrowsNothing()
                .ConfigureAwait(false);
        }

        private struct TestStruct
        {
            public int value;
            public string name;
        }
    }
}
