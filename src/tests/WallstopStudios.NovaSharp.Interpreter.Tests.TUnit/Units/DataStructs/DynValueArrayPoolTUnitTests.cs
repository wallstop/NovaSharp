namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataStructs
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Tests for <see cref="DynValueArrayPool"/> allocation reduction pool.
    /// </summary>
    public sealed class DynValueArrayPoolTUnitTests
    {
        [Test]
        public async Task GetReturnsEmptyArrayForZeroLength()
        {
            using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                0,
                out DynValue[] array
            );

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task GetReturnsEmptyArrayForNegativeLength()
        {
            using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                -1,
                out DynValue[] array
            );

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task GetReturnsArrayOfRequestedLengthForSmallArrays()
        {
            using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                5,
                out DynValue[] array
            );

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsEqualTo(5).ConfigureAwait(false);
        }

        [Test]
        public async Task GetReturnsArrayOfAtLeastRequestedLengthForLargeArrays()
        {
            using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                100,
                out DynValue[] array
            );

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsGreaterThanOrEqualTo(100).ConfigureAwait(false);
        }

        [Test]
        public async Task RentReturnsArrayOfRequestedLength()
        {
            using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                3,
                out DynValue[] array
            );

            await Assert.That(array).IsNotNull().ConfigureAwait(false);
            await Assert.That(array.Length).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task ReturnedArrayIsReused()
        {
            // Get an array
            DynValue[] first;
            using (PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(4, out first))
            {
                first[0] = DynValue.NewNumber(42);
            }

            // Get another array of the same size
            using PooledResource<DynValue[]> pooled2 = DynValueArrayPool.Get(
                4,
                out DynValue[] second
            );

            // Should be the same array (from pool) and cleared
            await Assert.That(second).IsSameReferenceAs(first).ConfigureAwait(false);
            await Assert.That(second[0]).IsNull().ConfigureAwait(false);
        }

        [Test]
        public async Task ToArrayAndReturnCreatesExactCopy()
        {
            DynValue[] rented = DynValueArrayPool.Rent(5);
            rented[0] = DynValue.NewNumber(1);
            rented[1] = DynValue.NewNumber(2);
            rented[2] = DynValue.NewNumber(3);

            DynValue[] result = DynValueArrayPool.ToArrayAndReturn(rented, 3);

            await Assert.That(result.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result[1].Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result[2].Number).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task ToArrayAndReturnReturnsEmptyForNullArray()
        {
            DynValue[] result = DynValueArrayPool.ToArrayAndReturn(null, 0);

            await Assert.That(result).IsNotNull().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ToArrayAndReturnReturnsEmptyForZeroLength()
        {
            DynValue[] rented = DynValueArrayPool.Rent(3);
            rented[0] = DynValue.NewNumber(1);

            DynValue[] result = DynValueArrayPool.ToArrayAndReturn(rented, 0);

            await Assert.That(result).IsNotNull().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task PooledResourceDisposeReturnsArray()
        {
            DynValue[] first;
            {
                PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(6, out first);
                pooled.Dispose();
            }

            // Next get should return the same array
            using PooledResource<DynValue[]> pooled2 = DynValueArrayPool.Get(
                6,
                out DynValue[] second
            );
            await Assert.That(second).IsSameReferenceAs(first).ConfigureAwait(false);
        }

        [Test]
        public async Task SuppressReturnPreventsPoolReturn()
        {
            DynValue[] first;
            {
                PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(7, out first);
                pooled.SuppressReturn();
                pooled.Dispose();
            }

            // Next get should NOT return the same array (it was suppressed)
            using PooledResource<DynValue[]> pooled2 = DynValueArrayPool.Get(
                7,
                out DynValue[] second
            );
            await Assert.That(second).IsNotSameReferenceAs(first).ConfigureAwait(false);
        }

        [Test]
        public async Task LargeArraysAreNotPooled()
        {
            // Arrays larger than MaxCachedLargeArraySize are not returned to pool
            DynValue[] first;
            using (PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(2000, out first))
            {
                // Just use it
            }

            using PooledResource<DynValue[]> pooled2 = DynValueArrayPool.Get(
                2000,
                out DynValue[] second
            );
            // Large arrays are not pooled, so should be different instances
            await Assert.That(second).IsNotSameReferenceAs(first).ConfigureAwait(false);
        }

        [Test]
        [Arguments(1)]
        [Arguments(2)]
        [Arguments(3)]
        [Arguments(4)]
        [Arguments(5)]
        [Arguments(6)]
        [Arguments(7)]
        [Arguments(8)]
        public async Task SmallArraySizesArePooled(int size)
        {
            DynValue[] first;
            using (PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(size, out first))
            {
                // Set some data
                first[0] = DynValue.NewNumber(size);
            }

            using PooledResource<DynValue[]> pooled2 = DynValueArrayPool.Get(
                size,
                out DynValue[] second
            );
            await Assert.That(second).IsSameReferenceAs(first).ConfigureAwait(false);
            // Should be cleared
            await Assert.That(second[0]).IsNull().ConfigureAwait(false);
        }

        [Test]
        public async Task GetOverloadWithoutOutReturnsPooledResource()
        {
            using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(4);

            await Assert.That(pooled.Resource).IsNotNull().ConfigureAwait(false);
            await Assert.That(pooled.Resource.Length).IsEqualTo(4).ConfigureAwait(false);
        }

        [Test]
        public async Task ReturnHandlesNullArray()
        {
            // Should not throw
            DynValueArrayPool.Return(null);
            // Test passes if no exception is thrown
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [Test]
        public async Task ReturnHandlesEmptyArray()
        {
            // Should not throw
            DynValueArrayPool.Return(Array.Empty<DynValue>());
            // Test passes if no exception is thrown
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [Test]
        public async Task ClearArrayParameterWorks()
        {
            DynValue[] array = DynValueArrayPool.Rent(3);
            array[0] = DynValue.NewNumber(1);
            array[1] = DynValue.NewNumber(2);

            // Return without clearing
            DynValueArrayPool.Return(array, clearArray: false);

            // Get the same array back
            DynValue[] returned = DynValueArrayPool.Rent(3);
            await Assert.That(returned).IsSameReferenceAs(array).ConfigureAwait(false);
            // Data should still be there (not cleared)
            await Assert.That(returned[0].Number).IsEqualTo(1).ConfigureAwait(false);

            DynValueArrayPool.Return(returned, clearArray: true);
        }
    }
}
