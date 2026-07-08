namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataStructs
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [NotInParallel(nameof(SharedPoolRegistry))]
    public sealed class MemoryPoolLifecycleTUnitTests
    {
        [Test]
        public async Task GenericPoolIdleTrimUsesDeterministicClock()
        {
            FakePoolClock clock = new();
            using GenericPool<object> pool = new(
                static () => new object(),
                maxPoolSize: 4,
                idleTimeout: TimeSpan.FromSeconds(60),
                clock: clock,
                name: "FakeClockPool"
            );

            object first;
            using (pool.Get(out first)) { }

            PoolStatistics before = pool.GetStatistics();
            PoolTrimResult early = pool.Trim(PoolTrimLevel.Idle);
            clock.Advance(TimeSpan.FromSeconds(61));
            PoolTrimResult afterIdle = pool.Trim(PoolTrimLevel.Idle);
            PoolStatistics after = pool.GetStatistics();

            await Assert.That(before.RetainedCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(early.TrimmedCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(afterIdle.TrimmedCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(after.RetainedCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task DisposedGenericPoolUnregistersFromSharedRegistry()
        {
            GenericPool<object> pool = new(
                static () => new object(),
                maxPoolSize: 1,
                name: "TransientTestPool"
            );

            bool afterCreate = SharedPoolRegistry.TestHooks.IsRegistered(pool);
            pool.Dispose();
            bool afterDispose = SharedPoolRegistry.TestHooks.IsRegistered(pool);

            await Assert.That(afterCreate).IsTrue().ConfigureAwait(false);
            await Assert.That(afterDispose).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task GenericPoolTrimHonorsRetainFloorsAndMaxTrimPerOperation()
        {
            using GenericPool<object> pool = new(
                static () => new object(),
                maxPoolSize: 6,
                name: "RetainFloorPool",
                minRetainCount: 1,
                warmRetainCount: 3,
                idleTimeout: TimeSpan.Zero,
                maxTrimPerOperation: 1
            );
            for (int i = 0; i < 5; i++)
            {
                pool.Return(new object());
            }

            PoolTrimResult firstIdle = pool.Trim(PoolTrimLevel.Idle);
            PoolStatistics afterFirstIdle = pool.GetStatistics();
            PoolTrimResult secondIdle = pool.Trim(PoolTrimLevel.Idle);
            PoolStatistics afterSecondIdle = pool.GetStatistics();
            PoolTrimResult thirdIdle = pool.Trim(PoolTrimLevel.Idle);
            PoolTrimResult critical = pool.Trim(PoolTrimLevel.Critical);
            PoolStatistics afterCritical = pool.GetStatistics();

            await Assert.That(firstIdle.TrimmedCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(afterFirstIdle.RetainedCount).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(secondIdle.TrimmedCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(afterSecondIdle.RetainedCount).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(thirdIdle.TrimmedCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(critical.TrimmedCount).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(afterCritical.RetainedCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [Test]
        public async Task ListPoolCriticalTrimDropsRetainedEntries()
        {
            GC.KeepAlive(new CriticalTrimProbe());
            ListPool<CriticalTrimProbe>.Return(ListPool<CriticalTrimProbe>.Rent());

            PoolTrimResult result = ListPool<CriticalTrimProbe>.Trim(PoolTrimLevel.Critical);
            PoolStatistics statistics = ListPool<CriticalTrimProbe>.GetStatistics();

            await Assert.That(result.TrimmedCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(statistics.RetainedCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ListPoolDropsOversizedCapacityOnReturn()
        {
            GC.KeepAlive(new CapacityDropProbe());
            List<CapacityDropProbe> oversized = ListPool<CapacityDropProbe>.Rent(4097);
            ListPool<CapacityDropProbe>.Return(oversized);

            PoolStatistics statistics = ListPool<CapacityDropProbe>.GetStatistics();
            List<CapacityDropProbe> rented = ListPool<CapacityDropProbe>.Rent();
            using DeferredActionScope cleanup = DeferredActionScope.Run(() =>
                ListPool<CapacityDropProbe>.Return(rented)
            );

            await Assert.That(statistics.RetainedCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(rented).IsNotSameReferenceAs(oversized).ConfigureAwait(false);
        }

        [Test]
        public async Task HashSetPoolDropsClearedOversizedCapacityOnReturn()
        {
            HashSetPool<CapacityDropProbe>.Trim(PoolTrimLevel.Critical);
            PooledResource<HashSet<CapacityDropProbe>> pooled = HashSetPool<CapacityDropProbe>.Get(
                out HashSet<CapacityDropProbe> oversized
            );
            for (int i = 0; i < 4097; i++)
            {
                oversized.Add(new CapacityDropProbe());
            }
            oversized.Clear();
            pooled.Dispose();

            PoolStatistics statistics = HashSetPool<CapacityDropProbe>.GetStatistics();
            using PooledResource<HashSet<CapacityDropProbe>> nextPooled =
                HashSetPool<CapacityDropProbe>.Get(out HashSet<CapacityDropProbe> rented);

            await Assert.That(statistics.RetainedCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(rented).IsNotSameReferenceAs(oversized).ConfigureAwait(false);
        }

        [Test]
        public async Task DictionaryPoolDropsClearedOversizedCapacityOnReturn()
        {
            DictionaryPool<CapacityDropProbe, CapacityDropProbe>.Trim(PoolTrimLevel.Critical);
            PooledResource<Dictionary<CapacityDropProbe, CapacityDropProbe>> pooled =
                DictionaryPool<CapacityDropProbe, CapacityDropProbe>.Get(
                    out Dictionary<CapacityDropProbe, CapacityDropProbe> oversized
                );
            for (int i = 0; i < 4097; i++)
            {
                oversized.Add(new CapacityDropProbe(), new CapacityDropProbe());
            }
            oversized.Clear();
            pooled.Dispose();

            PoolStatistics statistics = DictionaryPool<
                CapacityDropProbe,
                CapacityDropProbe
            >.GetStatistics();
            using PooledResource<Dictionary<CapacityDropProbe, CapacityDropProbe>> nextPooled =
                DictionaryPool<CapacityDropProbe, CapacityDropProbe>.Get(
                    out Dictionary<CapacityDropProbe, CapacityDropProbe> rented
                );

            await Assert.That(statistics.RetainedCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(rented).IsNotSameReferenceAs(oversized).ConfigureAwait(false);
        }

        [Test]
        public async Task StackPoolTrimsCapacityAndRetainsClearedInstanceOnReturn()
        {
            StackPool<CapacityDropProbe>.Trim(PoolTrimLevel.Critical);
            PooledResource<Stack<CapacityDropProbe>> pooled = StackPool<CapacityDropProbe>.Get(
                out Stack<CapacityDropProbe> oversized
            );
            for (int i = 0; i < 4097; i++)
            {
                oversized.Push(new CapacityDropProbe());
            }
            oversized.Clear();
            pooled.Dispose();

            PoolStatistics statistics = StackPool<CapacityDropProbe>.GetStatistics();
            using PooledResource<Stack<CapacityDropProbe>> nextPooled =
                StackPool<CapacityDropProbe>.Get(out Stack<CapacityDropProbe> rented);

            await Assert.That(statistics.RetainedCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(rented).IsSameReferenceAs(oversized).ConfigureAwait(false);
        }

        [Test]
        public async Task QueuePoolTrimsCapacityAndRetainsClearedInstanceOnReturn()
        {
            QueuePool<CapacityDropProbe>.Trim(PoolTrimLevel.Critical);
            PooledResource<Queue<CapacityDropProbe>> pooled = QueuePool<CapacityDropProbe>.Get(
                out Queue<CapacityDropProbe> oversized
            );
            for (int i = 0; i < 4097; i++)
            {
                oversized.Enqueue(new CapacityDropProbe());
            }
            oversized.Clear();
            pooled.Dispose();

            PoolStatistics statistics = QueuePool<CapacityDropProbe>.GetStatistics();
            using PooledResource<Queue<CapacityDropProbe>> nextPooled =
                QueuePool<CapacityDropProbe>.Get(out Queue<CapacityDropProbe> rented);

            await Assert.That(statistics.RetainedCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(rented).IsSameReferenceAs(oversized).ConfigureAwait(false);
        }

        [Test]
        public async Task CriticalTrimDoesNotAffectRentedCollection()
        {
            GC.KeepAlive(new RentedProbe());
            System.Collections.Generic.List<RentedProbe> rented = ListPool<RentedProbe>.Rent();

            PoolTrimResult trimWhileRented = ListPool<RentedProbe>.Trim(PoolTrimLevel.Critical);
            ListPool<RentedProbe>.Return(rented);
            System.Collections.Generic.List<RentedProbe> next = ListPool<RentedProbe>.Rent();
            using DeferredActionScope cleanup = DeferredActionScope.Run(() =>
                ListPool<RentedProbe>.Return(next)
            );

            await Assert.That(trimWhileRented.TrimmedCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(next).IsSameReferenceAs(rented).ConfigureAwait(false);
        }

        [Test]
        public async Task DynValueArrayPoolCriticalTrimClearsSmallBucket()
        {
            DynValue[] first = DynValueArrayPool.Rent(3);
            DynValueArrayPool.Return(first);

            DynValueArrayPool.Trim(PoolTrimLevel.Critical);
            DynValue[] second = DynValueArrayPool.Rent(3);
            using DeferredActionScope cleanup = DeferredActionScope.Run(() =>
                DynValueArrayPool.Return(second)
            );

            await Assert.That(second).IsNotSameReferenceAs(first).ConfigureAwait(false);
        }

        [Test]
        public async Task ObjectArrayPoolCriticalTrimClearsSmallBucket()
        {
            object[] first = ObjectArrayPool.Rent(2);
            ObjectArrayPool.Return(first);

            ObjectArrayPool.Trim(PoolTrimLevel.Critical);
            object[] second = ObjectArrayPool.Rent(2);
            using DeferredActionScope cleanup = DeferredActionScope.Run(() =>
                ObjectArrayPool.Return(second)
            );

            await Assert.That(second).IsNotSameReferenceAs(first).ConfigureAwait(false);
        }

        [Test]
        public async Task SmallArrayReplacementKeepsPeakBytesAtLeastCurrentRetainedBytes()
        {
            DynValueArrayPool.Trim(PoolTrimLevel.Critical);
            ObjectArrayPool.Trim(PoolTrimLevel.Critical);

            DynValueArrayPool.Return(new DynValue[3]);
            DynValueArrayPool.Return(new DynValue[3]);
            DynValueArrayPool.Return(new DynValue[4]);

            ObjectArrayPool.Return(new object[3]);
            ObjectArrayPool.Return(new object[3]);
            ObjectArrayPool.Return(new object[4]);

            PoolStatistics dynValueStatistics = DynValueArrayPool.GetStatistics();
            PoolStatistics objectStatistics = ObjectArrayPool.GetStatistics();

            await Assert
                .That(dynValueStatistics.PeakRetainedBytes)
                .IsGreaterThanOrEqualTo(dynValueStatistics.EstimatedRetainedBytes)
                .ConfigureAwait(false);
            await Assert
                .That(objectStatistics.PeakRetainedBytes)
                .IsGreaterThanOrEqualTo(objectStatistics.EstimatedRetainedBytes)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task RegistryCriticalTrimReachesWorkerThreadSmallArrayCache()
        {
            using ManualResetEventSlim returned = new(false);
            using ManualResetEventSlim trimmed = new(false);
            DynValue[] first = null;
            DynValue[] second = null;

            Task worker = Task.Run(() =>
            {
                first = DynValueArrayPool.Rent(4);
                DynValueArrayPool.Return(first);
                returned.Set();
                if (!trimmed.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("Timed out waiting for registry trim.");
                }

                second = DynValueArrayPool.Rent(4);
                DynValueArrayPool.Return(second);
            });
            bool returnedInTime = returned.Wait(TimeSpan.FromSeconds(10));

            SharedPoolRegistry.Trim(PoolTrimLevel.Critical);
            trimmed.Set();
            Task completed = await Task.WhenAny(worker, Task.Delay(TimeSpan.FromSeconds(10)))
                .ConfigureAwait(false);

            await Assert.That(returnedInTime).IsTrue().ConfigureAwait(false);
            await Assert.That(ReferenceEquals(completed, worker)).IsTrue().ConfigureAwait(false);
            await worker.ConfigureAwait(false);
            await Assert.That(second).IsNotSameReferenceAs(first).ConfigureAwait(false);
        }

        [Test]
        public async Task CallStackItemPoolCriticalTrimDropsReturnedFrames()
        {
            CallStackItemPool.Trim(PoolTrimLevel.Critical);
            CallStackItem first = CallStackItemPool.Rent();
            CallStackItem second = CallStackItemPool.Rent();
            CallStackItemPool.Return(first);
            CallStackItemPool.Return(second);

            CallStackItemPool.Trim(PoolTrimLevel.Critical);
            CallStackItem afterFirst = CallStackItemPool.Rent();
            CallStackItem afterSecond = CallStackItemPool.Rent();
            using DeferredActionScope cleanup = DeferredActionScope.Run(() =>
            {
                CallStackItemPool.Return(afterFirst);
                CallStackItemPool.Return(afterSecond);
            });

            bool reused =
                ReferenceEquals(afterFirst, first)
                || ReferenceEquals(afterFirst, second)
                || ReferenceEquals(afterSecond, first)
                || ReferenceEquals(afterSecond, second);

            await Assert.That(reused).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task CallStackItemPoolCriticalTrimDropsFramesReturnedByDeepRecursion()
        {
            CallStackItemPool.Trim(PoolTrimLevel.Critical);
            using LuaEngine lua = LuaEngine.Create();

            LuaValue result = await lua.RunAsync(
                    @"
local function recurse(n)
    if n == 0 then
        return 0
    end

    return 1 + recurse(n - 1)
end

return recurse(80)
",
                    "deep_recursion_pool.lua"
                )
                .ConfigureAwait(false);

            PoolStatistics afterRun = CallStackItemPool.GetStatistics();
            PoolTrimResult trim = CallStackItemPool.Trim(PoolTrimLevel.Critical);
            PoolStatistics afterTrim = CallStackItemPool.GetStatistics();

            await Assert.That(result.AsInteger()).IsEqualTo(80).ConfigureAwait(false);
            await Assert.That(afterRun.RetainedCount).IsGreaterThan(0).ConfigureAwait(false);
            await Assert.That(trim.TrimmedCount).IsGreaterThan(0).ConfigureAwait(false);
            await Assert.That(afterTrim.RetainedCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task SystemArrayPoolDropsArraysOverByteCap()
        {
            PoolStatistics before = SystemArrayPool<int>.GetStatistics();
            int[] array = SystemArrayPool<int>.Rent(300000);

            SystemArrayPool<int>.Return(array);

            PoolStatistics after = SystemArrayPool<int>.GetStatistics();
            await Assert
                .That(after.DroppedCount)
                .IsGreaterThan(before.DroppedCount)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task SystemArrayPoolSharedRetentionIsOpaqueToNovaSharpStatistics()
        {
            GC.KeepAlive(new OpaqueArrayProbe());
            PoolStatistics systemBefore = SystemArrayPool<OpaqueArrayProbe>.GetStatistics();

            OpaqueArrayProbe[] opaqueValues = SystemArrayPool<OpaqueArrayProbe>.Rent(1024);

            SystemArrayPool<OpaqueArrayProbe>.Return(opaqueValues);

            PoolStatistics systemAfter = SystemArrayPool<OpaqueArrayProbe>.GetStatistics();

            await Assert
                .That(systemAfter.DroppedCount)
                .IsEqualTo(systemBefore.DroppedCount)
                .ConfigureAwait(false);
            await Assert.That(systemAfter.RetainedCount).IsEqualTo(0).ConfigureAwait(false);
        }

        private sealed class FakePoolClock : IPoolClock
        {
            private long _ticks;

            public long GetTimestamp()
            {
                return _ticks;
            }

            public long ToTimestampTicks(TimeSpan duration)
            {
                return duration.Ticks;
            }

            internal void Advance(TimeSpan duration)
            {
                _ticks += duration.Ticks;
            }
        }

        private sealed class CriticalTrimProbe { }

        private sealed class CapacityDropProbe { }

        private sealed class RentedProbe { }

        private sealed class OpaqueArrayProbe { }
    }
}
