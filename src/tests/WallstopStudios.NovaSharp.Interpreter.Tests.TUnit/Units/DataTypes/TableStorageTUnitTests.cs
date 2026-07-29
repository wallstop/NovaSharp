namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Covers the host-visible contracts of the array-part plus hash-part table storage: key
    /// routing, traversal ordering, and how retained storage is reported to the allocation tracker.
    /// </summary>
    public sealed class TableStorageTUnitTests
    {
        private static Script CreateTrackedScript(LuaCompatibilityVersion version)
        {
            return new Script(
                new ScriptOptions
                {
                    CompatibilityVersion = version,
                    Sandbox = new SandboxOptions { MaxMemoryBytes = 16 * 1024 * 1024 },
                }
            );
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task NonPositiveHostIntegerKeysAddressTheSameEntryAsScript(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            Table globals = script.Globals;
            Table table = new(script);
            globals.Set("t", DynValue.FromTable(table));

            table.Set(0, DynValue.NewString("zero"));
            table.Set(-7, DynValue.NewString("minus-seven"));

            // The host integer overloads previously used a key space of their own, so these writes
            // were invisible to the script.
            DynValue seen = script.DoString("return t[0] .. '/' .. t[-7]");

            await Assert.That(seen.String).IsEqualTo("zero/minus-seven").ConfigureAwait(false);
            await Assert
                .That(table.RawGet(DynValue.NewNumber(0)).String)
                .IsEqualTo("zero")
                .ConfigureAwait(false);
            await Assert.That(table.Remove(0)).IsTrue().ConfigureAwait(false);
            await Assert.That(table.RawGet(0)).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TraversalYieldsArrayPartBeforeInsertionOrderedHashPart(
            LuaCompatibilityVersion version
        )
        {
            Table table = new(new Script(version));
            table.Set("first", DynValue.NewNumber(1));
            table.Set(1, DynValue.NewNumber(2));
            table.Set("second", DynValue.NewNumber(3));
            table.Set(2, DynValue.NewNumber(4));
            table.Set("third", DynValue.NewNumber(5));

            List<string> order = new();
            foreach (TablePair pair in table.GetPairsEnumerator())
            {
                order.Add(
                    pair.Key.Type == DataType.String
                        ? pair.Key.String
                        : pair.Key.Number.ToString(CultureInfo.InvariantCulture)
                );
            }

            await Assert
                .That(string.Join(",", order))
                .IsEqualTo("1,2,first,second,third")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task HashPartKeepsInsertionOrderAcrossGrowth(LuaCompatibilityVersion version)
        {
            Table table = new(new Script(version));

            // Enough string keys to force several rebuilds of the hash part.
            List<string> expected = new();
            for (int i = 0; i < 200; i++)
            {
                string key = "field" + i.ToString(CultureInfo.InvariantCulture);
                expected.Add(key);
                table.Set(key, DynValue.NewNumber(i));
            }

            List<string> actual = new();
            foreach (DynValue key in table.GetKeysEnumerator())
            {
                actual.Add(key.String);
            }

            await Assert
                .That(string.Join(",", actual))
                .IsEqualTo(string.Join(",", expected))
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RemovedKeyIsRejectedAsATraversalCursor(LuaCompatibilityVersion version)
        {
            Table table = new(new Script(version));
            table.Set("a", DynValue.NewNumber(1));
            table.Set("b", DynValue.NewNumber(2));

            TablePair? beforeRemoval = table.NextKey(DynValue.NewString("a"));
            await Assert.That(beforeRemoval.HasValue).IsTrue().ConfigureAwait(false);
            await Assert.That(beforeRemoval.Value.Key.String).IsEqualTo("b").ConfigureAwait(false);

            table.Remove("a");

            await Assert
                .That(table.NextKey(DynValue.NewString("a")).HasValue)
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task NilledKeyRemainsAValidTraversalCursor(LuaCompatibilityVersion version)
        {
            Table table = new(new Script(version));
            table.Set(1, DynValue.NewNumber(10));
            table.Set(2, DynValue.NewNumber(20));
            table.Set(3, DynValue.NewNumber(30));

            table.Set(2, DynValue.Nil);

            TablePair? next = table.NextKey(DynValue.NewNumber(2));

            await Assert.That(next.HasValue).IsTrue().ConfigureAwait(false);
            await Assert.That(next.Value.Key.Number).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SparseIntegerKeysRoundTripOutsideTheArrayPart(
            LuaCompatibilityVersion version
        )
        {
            Table table = new(new Script(version));
            for (int i = 1; i <= 32; i++)
            {
                table.Set(i, DynValue.NewNumber(i));
            }

            int[] sparse = { 1000, 250000, int.MaxValue };
            foreach (int key in sparse)
            {
                table.Set(key, DynValue.NewNumber(key));
            }

            for (int i = 1; i <= 32; i++)
            {
                await Assert.That(table.RawGet(i).Number).IsEqualTo(i).ConfigureAwait(false);
            }

            foreach (int key in sparse)
            {
                await Assert.That(table.RawGet(key).Number).IsEqualTo(key).ConfigureAwait(false);
            }

            await Assert.That(table.Length).IsEqualTo(32).ConfigureAwait(false);
            await Assert.That(table.Count).IsEqualTo(35).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RawGetOfANullStringKeyReportsAbsence(LuaCompatibilityVersion version)
        {
            Table table = new(new Script(version));
            table.Set("present", DynValue.NewNumber(1));

            await Assert.That(table.RawGet((string)null)).IsNull().ConfigureAwait(false);
            await Assert.That(table.Remove((string)null)).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task NillingEntriesKeepsRetainedStorageAccountedFor(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateTrackedScript(version);
            Table table = new(script);
            long empty = script.AllocationTracker.CurrentBytes;

            for (int i = 1; i <= 256; i++)
            {
                table.Set(i, DynValue.NewNumber(i));
            }

            long filled = script.AllocationTracker.CurrentBytes;
            await Assert.That(filled).IsGreaterThan(empty).ConfigureAwait(false);

            for (int i = 1; i <= 256; i++)
            {
                table.Set(i, DynValue.Nil);
            }

            // Writing nil does not hand the storage back, so a sandbox limit must keep counting it.
            await Assert
                .That(script.AllocationTracker.CurrentBytes)
                .IsEqualTo(filled)
                .ConfigureAwait(false);

            table.Clear();

            await Assert
                .That(script.AllocationTracker.CurrentBytes)
                .IsEqualTo(empty)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DenseIntegerStorageCostsFarLessThanOneEntryPerHashNode(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateTrackedScript(version);
            Table table = new(script);
            long empty = script.AllocationTracker.CurrentBytes;

            const int Entries = 4096;
            for (int i = 1; i <= Entries; i++)
            {
                table.Set(i, DynValue.NewNumber(i));
            }

            long perEntry = (script.AllocationTracker.CurrentBytes - empty) / Entries;

            // The array part stores one reference per slot; the previous linked-list storage charged
            // 64 bytes for every entry. Guard the order of magnitude, not an exact figure.
            await Assert.That(perEntry).IsLessThanOrEqualTo(24).ConfigureAwait(false);
            await Assert.That(table.Length).IsEqualTo(Entries).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CollectDeadKeysReleasesStorageWhenEverythingWasNil(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateTrackedScript(version);
            Table table = new(script);
            long empty = script.AllocationTracker.CurrentBytes;

            table.Set("a", DynValue.Nil);
            table.Set(1, DynValue.Nil);
            table.Set(DynValue.False, DynValue.Nil);
            await Assert.That(table.Count).IsEqualTo(3).ConfigureAwait(false);

            table.CollectDeadKeys();

            await Assert.That(table.Count).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(script.AllocationTracker.CurrentBytes)
                .IsEqualTo(empty)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ManyCollidingStringKeysStayIndividuallyAddressable(
            LuaCompatibilityVersion version
        )
        {
            // Structured keys are the shape that degenerates when a hash distributes badly in the
            // low bits, which is what the bucket table masks on.
            Table table = new(new Script(version));
            const int Count = 5000;
            for (int i = 0; i < Count; i++)
            {
                table.Set(
                    "entity_" + i.ToString(CultureInfo.InvariantCulture),
                    DynValue.NewNumber(i)
                );
            }

            for (int i = 0; i < Count; i++)
            {
                DynValue value = table.RawGet("entity_" + i.ToString(CultureInfo.InvariantCulture));
                await Assert.That(value).IsNotNull().ConfigureAwait(false);
                await Assert.That(value.Number).IsEqualTo(i).ConfigureAwait(false);
            }

            await Assert.That(table.Count).IsEqualTo(Count).ConfigureAwait(false);
        }
    }
}
