namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides boxing-free sort extension methods for <see cref="IList{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extension methods use generic <c>TComparer : IComparer&lt;T&gt;</c> constraints
    /// to avoid boxing when the comparer is a value type (struct). This eliminates the
    /// allocations that occur when using <c>List&lt;T&gt;.Sort(IComparer&lt;T&gt;)</c>
    /// with struct comparers.
    /// </para>
    /// <para>
    /// The implementation uses pattern-defeating quicksort (pdqsort):
    /// <list type="bullet">
    /// <item><description>Insertion sort for small arrays (≤24 elements) - O(n²) but cache-friendly</description></item>
    /// <item><description>Pattern-defeating quicksort for larger arrays - O(n log n) average</description></item>
    /// <item><description>Heapsort fallback when bad pivot sequences are detected</description></item>
    /// <item><description>Special handling for repeated elements and pre-sorted patterns</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Reference: Orson Peters' pdqsort algorithm (https://github.com/orlp/pdqsort)
    /// </para>
    /// </remarks>
    internal static class IListSortExtensions
    {
        /// <summary>
        /// Threshold below which insertion sort is used.
        /// pdqsort uses 24 as it provides better cache locality for the insertion sort phase.
        /// </summary>
        private const int InsertionSortThreshold = 24;

        /// <summary>
        /// When partition produces a segment with many equal elements, use this threshold
        /// to detect and optimize for repeated patterns.
        /// </summary>
        private const int NinthsThreshold = 128;

        /// <summary>
        /// Number of consecutive bad partitions before switching to heapsort.
        /// A partition is "bad" if it's highly unbalanced.
        /// </summary>
        private const int PartialInsertionSortLimit = 8;

        /// <summary>
        /// Sorts the elements in the list using the specified comparer without boxing.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="TComparer">The type of comparer (use a struct to avoid boxing).</typeparam>
        /// <param name="list">The list to sort.</param>
        /// <param name="comparer">The comparer to use for element comparisons.</param>
        /// <remarks>
        /// <para>Performance: O(n log n) average/worst case using pdqsort with heapsort fallback.</para>
        /// <para>Allocations: Zero allocations when TComparer is a value type.</para>
        /// <para>Stability: Not a stable sort - equal elements may be reordered.</para>
        /// </remarks>
        public static void Sort<T, TComparer>(this IList<T> list, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = list.Count;
            if (count < 2)
            {
                return;
            }

            int depthLimit = FloorLog2(count);
            PdqSort(list, 0, count, comparer, depthLimit, true);
        }

        /// <summary>
        /// Sorts a range of elements in the list using the specified comparer without boxing.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="TComparer">The type of comparer (use a struct to avoid boxing).</typeparam>
        /// <param name="list">The list to sort.</param>
        /// <param name="index">The starting index of the range to sort.</param>
        /// <param name="count">The number of elements in the range to sort.</param>
        /// <param name="comparer">The comparer to use for element comparisons.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when index or count are negative, or index + count exceeds the list size.
        /// </exception>
        public static void Sort<T, TComparer>(
            this IList<T> list,
            int index,
            int count,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative.");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
            }

            if (index + count > list.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    "Index and count must refer to a location within the list."
                );
            }

            if (count < 2)
            {
                return;
            }

            int depthLimit = FloorLog2(count);
            PdqSort(list, index, index + count, comparer, depthLimit, true);
        }

        /// <summary>
        /// Sorts the elements in the list using insertion sort.
        /// Efficient for small or nearly-sorted lists.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="TComparer">The type of comparer (use a struct to avoid boxing).</typeparam>
        /// <param name="list">The list to sort.</param>
        /// <param name="comparer">The comparer to use for element comparisons.</param>
        /// <remarks>
        /// <para>Performance: O(n²) worst/average case, O(n) best case when nearly sorted.</para>
        /// <para>Allocations: Zero allocations when TComparer is a value type.</para>
        /// <para>Stability: This is a stable sort - equal elements maintain their relative order.</para>
        /// </remarks>
        public static void InsertionSort<T, TComparer>(this IList<T> list, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = list.Count;
            if (count < 2)
            {
                return;
            }

            InsertionSortRange(list, 0, count - 1, comparer);
        }

        /// <summary>
        /// Pattern-defeating quicksort implementation.
        /// </summary>
        /// <remarks>
        /// pdqsort is an improvement over introsort that detects patterns in data:
        /// <list type="bullet">
        /// <item><description>Nearly sorted data: detected via partial insertion sort</description></item>
        /// <item><description>Many equal elements: detected via partition skew</description></item>
        /// <item><description>Adversarial patterns: handled via heapsort fallback</description></item>
        /// </list>
        /// </remarks>
        /// <param name="list">The list to sort.</param>
        /// <param name="begin">Start index (inclusive).</param>
        /// <param name="end">End index (exclusive).</param>
        /// <param name="comparer">The comparer to use.</param>
        /// <param name="badAllowed">Number of bad partitions allowed before heapsort fallback.</param>
        /// <param name="leftmost">Whether this is the leftmost partition (affects insertion sort variant).</param>
        private static void PdqSort<T, TComparer>(
            IList<T> list,
            int begin,
            int end,
            TComparer comparer,
            int badAllowed,
            bool leftmost
        )
            where TComparer : IComparer<T>
        {
            while (true)
            {
                int size = end - begin;

                // Use insertion sort for small partitions
                if (size < InsertionSortThreshold)
                {
                    if (leftmost)
                    {
                        InsertionSortRange(list, begin, end - 1, comparer);
                    }
                    else
                    {
                        // Unguarded insertion sort - we know there's a smaller element to the left
                        UnguardedInsertionSort(list, begin, end, comparer);
                    }
                    return;
                }

                // Calculate midpoint for pivot selection
                int halfSize = size >> 1;
                int mid = begin + halfSize;

                // Fallback to heapsort if we've had too many bad partitions
                if (badAllowed == 0)
                {
                    HeapSort(list, begin, end - 1, comparer);
                    return;
                }

                // For large arrays, use ninther (median of medians) for pivot selection
                // to be more robust against adversarial inputs
                if (size >= NinthsThreshold)
                {
                    SortThree(list, begin, begin + halfSize, end - 1, comparer);
                    SortThree(list, begin + 1, begin + halfSize - 1, end - 2, comparer);
                    SortThree(list, begin + 2, begin + halfSize + 1, end - 3, comparer);
                    SortThree(
                        list,
                        begin + halfSize - 1,
                        begin + halfSize,
                        begin + halfSize + 1,
                        comparer
                    );
                    Swap(list, begin, begin + halfSize);
                }
                else
                {
                    // Median of three pivot selection
                    SortThree(list, begin + halfSize, begin, end - 1, comparer);
                }

                // Partition with pivot at begin
                // Returns: (pivotPos, alreadyPartitioned)
                int pivotPos;
                bool alreadyPartitioned;
                PartitionRight(list, begin, end, comparer, out pivotPos, out alreadyPartitioned);

                // Check for many equal elements pattern
                int leftSize = pivotPos - begin;
                int rightSize = end - (pivotPos + 1);
                bool highlyUnbalanced = leftSize < size / 8 || rightSize < size / 8;

                if (highlyUnbalanced)
                {
                    badAllowed--;

                    // If the partition was highly unbalanced and we still have bad partitions left,
                    // try to break up equal element sequences with partition_left
                    if (badAllowed > 0)
                    {
                        // Swap elements at 1/4, 1/2, 3/4 positions to break up patterns
                        if (leftSize >= InsertionSortThreshold)
                        {
                            Swap(list, begin, begin + leftSize / 4);
                            Swap(list, pivotPos - 1, pivotPos - leftSize / 4);
                            if (leftSize > NinthsThreshold)
                            {
                                Swap(list, begin + 1, begin + leftSize / 4 + 1);
                                Swap(list, begin + 2, begin + leftSize / 4 + 2);
                                Swap(list, pivotPos - 2, pivotPos - leftSize / 4 - 1);
                                Swap(list, pivotPos - 3, pivotPos - leftSize / 4 - 2);
                            }
                        }

                        if (rightSize >= InsertionSortThreshold)
                        {
                            Swap(list, pivotPos + 1, pivotPos + 1 + rightSize / 4);
                            Swap(list, end - 1, end - rightSize / 4);
                            if (rightSize > NinthsThreshold)
                            {
                                Swap(list, pivotPos + 2, pivotPos + 2 + rightSize / 4);
                                Swap(list, pivotPos + 3, pivotPos + 3 + rightSize / 4);
                                Swap(list, end - 2, end - 1 - rightSize / 4);
                                Swap(list, end - 3, end - 2 - rightSize / 4);
                            }
                        }
                    }
                }
                else
                {
                    // If the partition was already partitioned and balanced,
                    // try partial insertion sort to detect nearly sorted data
                    if (
                        alreadyPartitioned
                        && PartialInsertionSort(list, begin, pivotPos, comparer)
                        && PartialInsertionSort(list, pivotPos + 1, end, comparer)
                    )
                    {
                        return;
                    }
                }

                // Recurse on the smaller partition, iterate on the larger
                if (leftSize < rightSize)
                {
                    PdqSort(list, begin, pivotPos, comparer, badAllowed, leftmost);
                    begin = pivotPos + 1;
                    leftmost = false;
                }
                else
                {
                    PdqSort(list, pivotPos + 1, end, comparer, badAllowed, false);
                    end = pivotPos;
                }
            }
        }

        /// <summary>
        /// Partitions the range [begin, end) around a pivot at begin.
        /// Elements less than pivot go to the left, elements greater or equal go to the right.
        /// </summary>
        /// <returns>The position where the pivot was placed, and whether the data was already partitioned.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PartitionRight<T, TComparer>(
            IList<T> list,
            int begin,
            int end,
            TComparer comparer,
            out int pivotPos,
            out bool alreadyPartitioned
        )
            where TComparer : IComparer<T>
        {
            T pivot = list[begin];

            int first = begin;
            int last = end;

            // Find first element >= pivot from the left
            while (comparer.Compare(list[++first], pivot) < 0)
            {
                // Guard against going past end
                if (first >= end - 1)
                {
                    break;
                }
            }

            // Find first element < pivot from the right
            if (first - 1 == begin)
            {
                while (first < last && comparer.Compare(list[--last], pivot) >= 0) { }
            }
            else
            {
                while (comparer.Compare(list[--last], pivot) >= 0) { }
            }

            // Check if already partitioned
            alreadyPartitioned = first >= last;

            // Partition loop
            while (first < last)
            {
                Swap(list, first, last);
                while (comparer.Compare(list[++first], pivot) < 0) { }
                while (comparer.Compare(list[--last], pivot) >= 0) { }
            }

            // Move pivot to its final position
            pivotPos = first - 1;
            list[begin] = list[pivotPos];
            list[pivotPos] = pivot;
        }

        /// <summary>
        /// Attempts to sort a nearly-sorted range using insertion sort.
        /// Stops early if too many moves are needed.
        /// </summary>
        /// <returns>True if successfully sorted, false if too many moves were needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool PartialInsertionSort<T, TComparer>(
            IList<T> list,
            int begin,
            int end,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            if (begin == end)
            {
                return true;
            }

            int limit = 0;
            for (int current = begin + 1; current != end; ++current)
            {
                if (limit > PartialInsertionSortLimit)
                {
                    return false;
                }

                T siftValue = list[current];
                int siftPos = current;

                // Find insertion position and count moves
                while (siftPos != begin && comparer.Compare(list[siftPos - 1], siftValue) > 0)
                {
                    list[siftPos] = list[siftPos - 1];
                    --siftPos;
                    ++limit;
                }

                list[siftPos] = siftValue;
            }

            return true;
        }

        /// <summary>
        /// Unguarded insertion sort - assumes there's a smaller element before the range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UnguardedInsertionSort<T, TComparer>(
            IList<T> list,
            int begin,
            int end,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            if (begin == end)
            {
                return;
            }

            for (int current = begin + 1; current != end; ++current)
            {
                T siftValue = list[current];
                int siftPos = current;

                // No bounds check needed - there's always a smaller element to the left
                while (comparer.Compare(list[siftPos - 1], siftValue) > 0)
                {
                    list[siftPos] = list[siftPos - 1];
                    --siftPos;
                }

                list[siftPos] = siftValue;
            }
        }

        /// <summary>
        /// Sorts three elements and places the median at the first position (a).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SortThree<T, TComparer>(
            IList<T> list,
            int a,
            int b,
            int c,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            // Sort b, c
            if (comparer.Compare(list[b], list[c]) > 0)
            {
                Swap(list, b, c);
            }

            // Sort a, c
            if (comparer.Compare(list[a], list[c]) > 0)
            {
                Swap(list, a, c);
            }

            // Sort a, b - after this, median is at position a
            if (comparer.Compare(list[a], list[b]) > 0)
            {
                Swap(list, a, b);
            }
        }

        /// <summary>
        /// Heapsort implementation for fallback when quicksort depth limit is exceeded.
        /// </summary>
        private static void HeapSort<T, TComparer>(
            IList<T> list,
            int left,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int length = right - left + 1;

            // Build max heap
            for (int i = (length >> 1) - 1; i >= 0; i--)
            {
                SiftDown(list, left, i, length, comparer);
            }

            // Extract elements from heap one by one
            for (int i = length - 1; i > 0; i--)
            {
                Swap(list, left, left + i);
                SiftDown(list, left, 0, i, comparer);
            }
        }

        /// <summary>
        /// Sift down operation for heapsort.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SiftDown<T, TComparer>(
            IList<T> list,
            int offset,
            int root,
            int length,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            while (true)
            {
                int leftChild = (root << 1) + 1;
                if (leftChild >= length)
                {
                    return;
                }

                int rightChild = leftChild + 1;
                int largest = root;

                if (comparer.Compare(list[offset + leftChild], list[offset + largest]) > 0)
                {
                    largest = leftChild;
                }

                if (
                    rightChild < length
                    && comparer.Compare(list[offset + rightChild], list[offset + largest]) > 0
                )
                {
                    largest = rightChild;
                }

                if (largest == root)
                {
                    return;
                }

                Swap(list, offset + root, offset + largest);
                root = largest;
            }
        }

        /// <summary>
        /// Insertion sort implementation for a range of elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InsertionSortRange<T, TComparer>(
            IList<T> list,
            int left,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            for (int i = left + 1; i <= right; i++)
            {
                T key = list[i];
                int j = i - 1;

                while (j >= left && comparer.Compare(list[j], key) > 0)
                {
                    list[j + 1] = list[j];
                    j--;
                }

                list[j + 1] = key;
            }
        }

        /// <summary>
        /// Swaps two elements in a list.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap<T>(IList<T> list, int a, int b)
        {
            T temp = list[a];
            list[a] = list[b];
            list[b] = temp;
        }

        /// <summary>
        /// Computes floor(log2(value)) for depth limit calculation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FloorLog2(int value)
        {
            int result = 0;
            while (value > 1)
            {
                value >>= 1;
                result++;
            }
            return result;
        }
    }
}
