namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;

    /// <summary>
    /// Tests for <see cref="CallStackItemPools"/> helper methods.
    /// </summary>
    public sealed class CallStackItemPoolsTUnitTests
    {
        [Test]
        public async Task GetBlocksToCloseListReturnsPooledList()
        {
            using PooledResource<List<List<SymbolRef>>> pooled =
                CallStackItemPools.GetBlocksToCloseList(out List<List<SymbolRef>> list);

            await Assert.That(list).IsNotNull().ConfigureAwait(false);
            await Assert.That(list).IsTypeOf<List<List<SymbolRef>>>().ConfigureAwait(false);
        }

        [Test]
        public async Task GetBlocksToCloseListReturnsEmptyList()
        {
            using PooledResource<List<List<SymbolRef>>> pooled =
                CallStackItemPools.GetBlocksToCloseList(out List<List<SymbolRef>> list);

            await Assert.That(list.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task GetBlocksToCloseListCanBeUsedMultipleTimes()
        {
            using PooledResource<List<List<SymbolRef>>> pooled1 =
                CallStackItemPools.GetBlocksToCloseList(out List<List<SymbolRef>> list1);
            list1.Add(new List<SymbolRef>());

            using PooledResource<List<List<SymbolRef>>> pooled2 =
                CallStackItemPools.GetBlocksToCloseList(out List<List<SymbolRef>> list2);

            await Assert.That(list2.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task GetBlocksToCloseListReturnsToPoolOnDispose()
        {
            List<List<SymbolRef>> list;
            using (
                PooledResource<List<List<SymbolRef>>> pooled =
                    CallStackItemPools.GetBlocksToCloseList(out list)
            )
            {
                list.Add(new List<SymbolRef>());
            }

            // Get another one - might be the same instance (cleared)
            using PooledResource<List<List<SymbolRef>>> pooled2 =
                CallStackItemPools.GetBlocksToCloseList(out List<List<SymbolRef>> list2);

            // Either it's a new list or the same one cleared
            await Assert.That(list2.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task GetClosersListReturnsPooledList()
        {
            using PooledResource<List<SymbolRef>> pooled = CallStackItemPools.GetClosersList(
                out List<SymbolRef> list
            );

            await Assert.That(list).IsNotNull().ConfigureAwait(false);
            await Assert.That(list).IsTypeOf<List<SymbolRef>>().ConfigureAwait(false);
        }

        [Test]
        public async Task GetClosersListReturnsEmptyList()
        {
            using PooledResource<List<SymbolRef>> pooled = CallStackItemPools.GetClosersList(
                out List<SymbolRef> list
            );

            await Assert.That(list.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task GetClosersListCanAddSymbolRefs()
        {
            using PooledResource<List<SymbolRef>> pooled = CallStackItemPools.GetClosersList(
                out List<SymbolRef> list
            );

            SymbolRef sym1 = SymbolRef.Local("test1", 0);
            SymbolRef sym2 = SymbolRef.Local("test2", 1);

            list.Add(sym1);
            list.Add(sym2);

            await Assert.That(list.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(list[0].Name).IsEqualTo("test1").ConfigureAwait(false);
            await Assert.That(list[1].Name).IsEqualTo("test2").ConfigureAwait(false);
        }

        [Test]
        public async Task GetClosersListWithCapacityReturnsListWithCapacity()
        {
            using PooledResource<List<SymbolRef>> pooled = CallStackItemPools.GetClosersList(
                10,
                out List<SymbolRef> list
            );

            await Assert.That(list).IsNotNull().ConfigureAwait(false);
            await Assert.That(list.Count).IsEqualTo(0).ConfigureAwait(false);
            // List has at least the requested capacity
            await Assert.That(list.Capacity).IsGreaterThanOrEqualTo(10).ConfigureAwait(false);
        }

        [Test]
        public async Task GetToBeClosedSetReturnsPooledHashSet()
        {
            using PooledResource<HashSet<int>> pooled = CallStackItemPools.GetToBeClosedSet(
                out HashSet<int> set
            );

            await Assert.That(set).IsNotNull().ConfigureAwait(false);
            await Assert.That(set).IsTypeOf<HashSet<int>>().ConfigureAwait(false);
        }

        [Test]
        public async Task GetToBeClosedSetReturnsEmptySet()
        {
            using PooledResource<HashSet<int>> pooled = CallStackItemPools.GetToBeClosedSet(
                out HashSet<int> set
            );

            await Assert.That(set.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task GetToBeClosedSetCanAddIntegers()
        {
            using PooledResource<HashSet<int>> pooled = CallStackItemPools.GetToBeClosedSet(
                out HashSet<int> set
            );

            set.Add(0);
            set.Add(1);
            set.Add(2);

            await Assert.That(set.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(set.Contains(0)).IsTrue().ConfigureAwait(false);
            await Assert.That(set.Contains(1)).IsTrue().ConfigureAwait(false);
            await Assert.That(set.Contains(2)).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task GetToBeClosedSetHandlesDuplicates()
        {
            using PooledResource<HashSet<int>> pooled = CallStackItemPools.GetToBeClosedSet(
                out HashSet<int> set
            );

            set.Add(5);
            set.Add(5);

            await Assert.That(set.Count).IsEqualTo(1).ConfigureAwait(false);
        }

        [Test]
        public async Task GetClosersListFromCopiesNonNullSource()
        {
            SymbolRef[] source = new[]
            {
                SymbolRef.Local("a", 0),
                SymbolRef.Local("b", 1),
                SymbolRef.Local("c", 2),
            };

            using PooledResource<List<SymbolRef>> pooled = CallStackItemPools.GetClosersListFrom(
                source,
                out List<SymbolRef> list
            );

            await Assert.That(list.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(list[0].Name).IsEqualTo("a").ConfigureAwait(false);
            await Assert.That(list[1].Name).IsEqualTo("b").ConfigureAwait(false);
            await Assert.That(list[2].Name).IsEqualTo("c").ConfigureAwait(false);
        }

        [Test]
        public async Task GetClosersListFromCreatesIndependentCopy()
        {
            SymbolRef[] source = new[] { SymbolRef.Local("original", 0) };

            using PooledResource<List<SymbolRef>> pooled = CallStackItemPools.GetClosersListFrom(
                source,
                out List<SymbolRef> list
            );

            // Modify source (though arrays are reference types, list is separate)
            source[0] = SymbolRef.Local("modified", 1);

            // Pooled list keeps original values
            await Assert.That(list[0].Name).IsEqualTo("original").ConfigureAwait(false);
        }

        [Test]
        public async Task MultiplePooledResourcesCanCoexist()
        {
            using PooledResource<List<List<SymbolRef>>> blocks =
                CallStackItemPools.GetBlocksToCloseList(out List<List<SymbolRef>> blocksList);
            using PooledResource<List<SymbolRef>> closers = CallStackItemPools.GetClosersList(
                out List<SymbolRef> closersList
            );
            using PooledResource<HashSet<int>> toBeClosed = CallStackItemPools.GetToBeClosedSet(
                out HashSet<int> toBeClosedSet
            );

            blocksList.Add(new List<SymbolRef>());
            closersList.Add(SymbolRef.Local("x", 0));
            toBeClosedSet.Add(42);

            await Assert.That(blocksList.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(closersList.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(toBeClosedSet.Count).IsEqualTo(1).ConfigureAwait(false);
        }

        [Test]
        public async Task PooledResourcesAreClearedOnReturn()
        {
            // Get first set
            using (
                PooledResource<List<List<SymbolRef>>> pooled =
                    CallStackItemPools.GetBlocksToCloseList(out List<List<SymbolRef>> list)
            )
            {
                list.Add(new List<SymbolRef>());
                list.Add(new List<SymbolRef>());
            }

            // Get another - should be empty regardless of whether it's recycled
            using PooledResource<List<List<SymbolRef>>> pooled2 =
                CallStackItemPools.GetBlocksToCloseList(out List<List<SymbolRef>> list2);

            await Assert.That(list2.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task NestedPooledResourcesWorkCorrectly()
        {
            using PooledResource<List<SymbolRef>> outer = CallStackItemPools.GetClosersList(
                out List<SymbolRef> outerList
            );
            outerList.Add(SymbolRef.Local("outer", 0));

            using (
                PooledResource<List<SymbolRef>> inner = CallStackItemPools.GetClosersList(
                    out List<SymbolRef> innerList
                )
            )
            {
                innerList.Add(SymbolRef.Local("inner", 1));
                await Assert.That(innerList.Count).IsEqualTo(1).ConfigureAwait(false);
            }

            // Outer should still have its value
            await Assert.That(outerList.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(outerList[0].Name).IsEqualTo("outer").ConfigureAwait(false);
        }
    }
}
