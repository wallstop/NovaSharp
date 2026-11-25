// The indexed quicksort is adapted from PUC Lua 5.3–5.5 (src/ltablib.c).
// Copyright (C) 1994-2025 Lua.org, PUC-Rio.
// The Lua MIT notice is included in the repository LICENSE.

namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;

    internal static partial class TableModule
    {
        // Lua 5.3 and later expose reads and writes during partitioning, including through
        // comparator side effects. Keep the sequence live and the swap order observable.
        private readonly struct IndexedTableSort
        {
            private readonly ScriptExecutionContext _context;
            private readonly TableSequence _sequence;
            private readonly LuaValue _comparator;

            public IndexedTableSort(
                ScriptExecutionContext context,
                TableSequence sequence,
                LuaValue comparator
            )
            {
                _context = context;
                _sequence = sequence;
                _comparator = comparator;
            }

            public void Sort(int lower, int upper, uint pivotSeed)
            {
                while (lower < upper)
                {
                    LuaValue lowValue = _sequence.Get(lower);
                    LuaValue highValue = _sequence.Get(upper);
                    if (Less(highValue, lowValue))
                    {
                        Swap(lower, lowValue, upper, highValue);
                    }

                    int width = upper - lower;
                    if (width == 1)
                    {
                        return;
                    }

                    int middle = ChoosePivot(lower, upper, pivotSeed);
                    LuaValue middleValue = _sequence.Get(middle);
                    lowValue = _sequence.Get(lower);
                    if (Less(middleValue, lowValue))
                    {
                        Swap(middle, middleValue, lower, lowValue);
                    }
                    else
                    {
                        highValue = _sequence.Get(upper);
                        if (Less(highValue, middleValue))
                        {
                            Swap(middle, middleValue, upper, highValue);
                        }
                    }

                    if (width == 2)
                    {
                        return;
                    }

                    LuaValue pivot = _sequence.Get(middle);
                    Swap(middle, pivot, upper - 1, _sequence.Get(upper - 1));
                    int partition = Partition(lower, upper, pivot);

                    // Recurse into the smaller interval and iterate over the larger one.
                    int leftWidth = partition - lower;
                    int rightWidth = upper - partition;
                    int smallerWidth;
                    if (leftWidth < rightWidth)
                    {
                        Sort(lower, partition - 1, pivotSeed);
                        lower = partition + 1;
                        smallerWidth = leftWidth;
                    }
                    else
                    {
                        Sort(partition + 1, upper, pivotSeed);
                        upper = partition - 1;
                        smallerWidth = rightWidth;
                    }

                    // Vary later pivots after a highly unbalanced partition without consuming
                    // the script's random sequence or depending on a clock.
                    if ((upper - lower) / 128 > smallerWidth)
                    {
                        pivotSeed = unchecked(pivotSeed * 1664525u + 1013904223u);
                    }
                }
            }

            private static int ChoosePivot(int lower, int upper, uint seed)
            {
                int width = upper - lower;
                if (width < 100 || seed == 0)
                {
                    return lower + width / 2;
                }

                int quarter = width / 4;
                uint mixedSeed = seed ^ (uint)lower ^ (uint)upper;
                return lower + quarter + (int)(mixedSeed % (uint)(quarter * 2));
            }

            private int Partition(int lower, int upper, LuaValue pivot)
            {
                int left = lower;
                int right = upper - 1;
                while (true)
                {
                    LuaValue leftValue = _sequence.Get(++left);
                    while (Less(leftValue, pivot))
                    {
                        if (left == upper - 1)
                        {
                            throw new ScriptRuntimeException("invalid order function for sorting");
                        }

                        leftValue = _sequence.Get(++left);
                    }

                    LuaValue rightValue = _sequence.Get(--right);
                    while (Less(pivot, rightValue))
                    {
                        if (right < left)
                        {
                            throw new ScriptRuntimeException("invalid order function for sorting");
                        }

                        rightValue = _sequence.Get(--right);
                    }

                    if (right < left)
                    {
                        Swap(upper - 1, pivot, left, leftValue);
                        return left;
                    }

                    Swap(left, leftValue, right, rightValue);
                }
            }

            private bool Less(LuaValue left, LuaValue right)
            {
                return _comparator.IsNil
                    ? _context.CompareLibraryLessThan(left, right)
                    : _context
                        .Script.CallLibraryCallback(_comparator, left, right)
                        .ToScalar()
                        .CastToBool();
            }

            private void Swap(int first, LuaValue firstValue, int second, LuaValue secondValue)
            {
                _sequence.Set(first, secondValue);
                _sequence.Set(second, firstValue);
            }
        }
    }
}
