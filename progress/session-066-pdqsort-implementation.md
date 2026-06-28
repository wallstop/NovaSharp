# Session 066: Pattern-Defeating Quicksort Implementation

**Date**: 2024-12-21
**Initiative**: 15 - Boxing-Free IList Sort Extensions
**Status**: ✅ Complete

## Summary

Implemented pattern-defeating quicksort (pdqsort) for `IListSortExtensions`, providing boxing-free sorting with excellent performance on patterned data.

## Problem

`List<T>.Sort(IComparer<T>)` and `Array.Sort(T[], IComparer<T>)` box the comparer when it's a value type (struct), causing allocations even with `readonly struct` comparers. Additionally, standard introsort doesn't handle patterned data (sorted, reverse-sorted, repeated elements) as efficiently as possible.

## Solution

Implemented pdqsort (based on Orson Peters' algorithm) which:

1. **Zero boxing allocations** via generic `TComparer : IComparer<T>` constraint
1. **Pattern detection** for nearly-sorted data
1. **Adversarial resistance** via ninther pivot selection
1. **Guaranteed O(n log n)** via heapsort fallback

## Algorithm Components

### 1. PdqSort Main Loop

```csharp
private static void PdqSort<T, TComparer>(
    IList<T> list, int begin, int end, TComparer comparer,
    int badAllowed, bool leftmost)
```

Uses iterative approach with tail-recursion optimization:

- Recurse on smaller partition, iterate on larger
- Track `badAllowed` counter for heapsort fallback
- `leftmost` flag controls insertion sort variant

### 2. Insertion Sort (threshold = 24)

- **Guarded**: For leftmost partition with bounds checking
- **Unguarded**: For interior partitions (element to left is always smaller)

### 3. Pivot Selection

- **Median-of-three** for small arrays
- **Ninther** (median of medians) for large arrays (≥128)

### 4. Partition with Pattern Detection

```csharp
private static void PartitionRight<T, TComparer>(
    IList<T> list, int begin, int end, TComparer comparer,
    out int pivotPos, out bool alreadyPartitioned)
```

Returns whether data was already partitioned (indicates sorted pattern).

### 5. Bad Partition Handling

When partition is highly unbalanced (< size/8 on either side):

- Decrement `badAllowed` counter
- Shuffle elements at 1/4, 1/2, 3/4 positions to break up patterns
- Switch to heapsort when counter reaches 0

### 6. Nearly-Sorted Detection

```csharp
private static bool PartialInsertionSort<T, TComparer>(...)
```

Attempts insertion sort with early termination (limit = 8 moves).
If data is nearly sorted, completes in O(n) time.

## Performance Characteristics

| Input Pattern     | Time Complexity | Notes                                |
| ----------------- | --------------- | ------------------------------------ |
| Random            | O(n log n)      | Standard quicksort behavior          |
| Already sorted    | O(n)            | Detected by partial insertion sort   |
| Reverse sorted    | O(n)            | Detected by partial insertion sort   |
| Few unique values | O(n log n)      | Better pivot selection reduces swaps |
| Adversarial       | O(n log n)      | Heapsort fallback guarantees         |

## Integration

Used by `TableModule.table_sort` for Lua's `table.sort` function:

```csharp
values.Sort(comparer);  // Uses IListSortExtensions.Sort<T, TComparer>
```

## Constants

| Constant                    | Value | Purpose                                 |
| --------------------------- | ----- | --------------------------------------- |
| `InsertionSortThreshold`    | 24    | Below this, use insertion sort          |
| `NinthsThreshold`           | 128   | Above this, use ninther pivot selection |
| `PartialInsertionSortLimit` | 8     | Max moves for partial insertion sort    |

## Test Results

All 11,754 tests pass including:

- 30 `table.sort`-specific tests
- All Lua version combinations (5.1-5.5)
- Edge cases: empty, single element, sorted, reverse sorted, repeated

## Files Modified

1. [IListSortExtensions.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/IListSortExtensions.cs)
   - Replaced introsort with pdqsort
   - Added `PdqSort`, `PartitionRight`, `PartialInsertionSort`, `UnguardedInsertionSort`
   - Updated `SortThree` to place median at first position

## References

- [Orson Peters' pdqsort](https://github.com/orlp/pdqsort)
- [Pattern-defeating Quicksort paper](https://arxiv.org/abs/2106.05123)
