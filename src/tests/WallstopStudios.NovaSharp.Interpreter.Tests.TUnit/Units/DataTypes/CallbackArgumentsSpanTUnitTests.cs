namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Tests for <see cref="CallbackArguments"/> span-based access methods
    /// (TryGetSpan, CopyTo).
    /// </summary>
    public sealed class CallbackArgumentsSpanTUnitTests
    {
        // Helper struct to capture span test results (spans can't be used in async methods in C# 12)
        private readonly struct TryGetSpanResult
        {
            public bool Success { get; }
            public int Length { get; }
            public double[] Numbers { get; }

            public TryGetSpanResult(bool success, int length, double[] numbers = null)
            {
                Success = success;
                Length = length;
                Numbers = numbers ?? Array.Empty<double>();
            }
        }

        private readonly struct CopyToResult
        {
            public int Count { get; }
            public double[] Numbers { get; }

            public CopyToResult(int count, double[] numbers)
            {
                Count = count;
                Numbers = numbers;
            }
        }

        private readonly struct SubrangeResult
        {
            public bool NegativeRawGetIsNull { get; }
            public DataType NegativeIndexerType { get; }
            public double First { get; }
            public double Second { get; }

            public SubrangeResult(
                bool negativeRawGetIsNull,
                DataType negativeIndexerType,
                double first,
                double second
            )
            {
                NegativeRawGetIsNull = negativeRawGetIsNull;
                NegativeIndexerType = negativeIndexerType;
                First = first;
                Second = second;
            }
        }

        private readonly struct NullStoredArgumentResult
        {
            public int Count { get; }
            public DataType RawType { get; }
            public DataType IndexerType { get; }
            public DataType CopyType { get; }

            public NullStoredArgumentResult(
                int count,
                DataType rawType,
                DataType indexerType,
                DataType copyType
            )
            {
                Count = count;
                RawType = rawType;
                IndexerType = indexerType;
                CopyType = copyType;
            }
        }

        private readonly struct TryGetSpanMetadata
        {
            public bool Success { get; }
            public int Length { get; }

            public TryGetSpanMetadata(bool success, int length)
            {
                Success = success;
                Length = length;
            }
        }

        private static TryGetSpanResult ExecuteTryGetSpan(CallbackArguments args)
        {
            bool result = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
            double[] numbers = new double[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                numbers[i] = span[i].Number;
            }
            return new TryGetSpanResult(result, span.Length, numbers);
        }

        private static TryGetSpanResult ExecuteViewTryGetSpan(DynValue[] backing)
        {
            CallbackArgumentsView args = new((IList<DynValue>)backing, false);
            bool result = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
            double[] numbers = new double[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                numbers[i] = span[i].Number;
            }
            return new TryGetSpanResult(result, span.Length, numbers);
        }

        private static TryGetSpanResult ExecuteFastStackTryGetSpan(
            FastStack<DynValue> stack,
            int offset,
            int count
        )
        {
            bool result = stack.TryGetSpan(offset, count, out ReadOnlySpan<DynValue> span);
            double[] numbers = new double[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                numbers[i] = span[i].Number;
            }

            return new TryGetSpanResult(result, span.Length, numbers);
        }

        private static TryGetSpanResult ExecuteFastStackDynamicTryGetSpan(
            FastStackDynamic<DynValue> stack,
            int offset,
            int count
        )
        {
            bool result = stack.TryGetSpan(offset, count, out ReadOnlySpan<DynValue> span);
            return new TryGetSpanResult(result, span.Length);
        }

        private static SubrangeResult ExecuteViewSubrange(FastStackDynamic<DynValue> backing)
        {
            CallbackArgumentsView args = new(backing, offset: 1, count: 2, isMethodCall: false);
            return new SubrangeResult(
                args.RawGet(-1, translateVoids: true) == null,
                args[-1].Type,
                args[0].Number,
                args[1].Number
            );
        }

        private static NullStoredArgumentResult ExecuteNullStoredArgumentView()
        {
            CallbackArgumentsView args = new(DynValue.NewNumber(1), null, isMethodCall: false);
            DynValue raw = args.RawGet(1, translateVoids: false);
            DynValue value = args[1];
            DynValue[] buffer = new DynValue[args.Count];
            args.CopyTo(buffer);

            return new NullStoredArgumentResult(args.Count, raw.Type, value.Type, buffer[1].Type);
        }

        private static NullStoredArgumentResult ExecuteNullTupleExpansion(bool useArgumentView)
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(10), null);
            DynValue[] backing = new[] { DynValue.NewNumber(1), tuple };

            if (useArgumentView)
            {
                CallbackArgumentsView args = new((IList<DynValue>)backing, false);
                DynValue raw = args.RawGet(2, translateVoids: false);
                DynValue value = args[2];
                DynValue[] buffer = new DynValue[args.Count];
                args.CopyTo(buffer);

                return new NullStoredArgumentResult(
                    args.Count,
                    raw.Type,
                    value.Type,
                    buffer[2].Type
                );
            }
            else
            {
                CallbackArguments args = new(backing, false);
                DynValue raw = args.RawGet(2, translateVoids: false);
                DynValue value = args[2];
                DynValue[] buffer = new DynValue[args.Count];
                args.CopyTo(buffer);

                return new NullStoredArgumentResult(
                    args.Count,
                    raw.Type,
                    value.Type,
                    buffer[2].Type
                );
            }
        }

        private static TryGetSpanMetadata ExecuteTryGetSpanMetadata(CallbackArguments args)
        {
            bool success = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
            return new TryGetSpanMetadata(success, span.Length);
        }

        private static TryGetSpanMetadata ExecuteViewTryGetSpanMetadata(DynValue[] backing)
        {
            CallbackArgumentsView args = new((IList<DynValue>)backing, false);
            bool success = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
            return new TryGetSpanMetadata(success, span.Length);
        }

        private static CopyToResult ExecuteCopyTo(
            CallbackArguments args,
            int bufferSize,
            int skip = 0
        )
        {
            DynValue[] buffer = new DynValue[bufferSize];
            int count =
                skip == 0 ? args.CopyTo(buffer.AsSpan()) : args.CopyTo(buffer.AsSpan(), skip);
            double[] numbers = new double[count];
            for (int i = 0; i < count; i++)
            {
                numbers[i] = buffer[i].Number;
            }
            return new CopyToResult(count, numbers);
        }

        private static CopyToResult ExecuteViewCopyTo(
            IList<DynValue> backing,
            int bufferSize,
            int skip = 0
        )
        {
            CallbackArgumentsView args = new(backing, false);
            DynValue[] buffer = new DynValue[bufferSize];
            int count =
                skip == 0 ? args.CopyTo(buffer.AsSpan()) : args.CopyTo(buffer.AsSpan(), skip);
            double[] numbers = new double[count];
            for (int i = 0; i < count; i++)
            {
                numbers[i] = buffer[i].Number;
            }
            return new CopyToResult(count, numbers);
        }

        private static CopyToResult ExecuteViewSkipMethodCall(DynValue[] backing)
        {
            CallbackArgumentsView args = new((IList<DynValue>)backing, true);
            CallbackArgumentsView skipped = args.SkipMethodCall();
            DynValue[] buffer = new DynValue[skipped.Count];
            int count = skipped.CopyTo(buffer.AsSpan());
            double[] numbers = new double[count];
            for (int i = 0; i < count; i++)
            {
                numbers[i] = buffer[i].Number;
            }
            return new CopyToResult(count, numbers);
        }

        [Test]
        public async Task TryGetSpanReturnsFalseForEmptyList()
        {
            List<DynValue> args = new();
            CallbackArguments callbackArgs = new(args, false);

            TryGetSpanResult result = ExecuteTryGetSpan(callbackArgs);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsTrueForArray()
        {
            DynValue[] backing = new[]
            {
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
            };
            CallbackArguments args = new(backing, false);

            TryGetSpanResult result = ExecuteTryGetSpan(args);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewTryGetSpanReturnsTrueForArray()
        {
            DynValue[] backing = new[]
            {
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
            };

            TryGetSpanResult result = ExecuteViewTryGetSpan(backing);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewSubrangeDoesNotExposeValuesBeforeOffset()
        {
            FastStackDynamic<DynValue> backing = new(startingCapacity: 4);
            backing.Push(DynValue.NewString("function-slot"));
            backing.Push(DynValue.NewNumber(10));
            backing.Push(DynValue.NewNumber(20));

            SubrangeResult result = ExecuteViewSubrange(backing);

            await Assert.That(result.NegativeRawGetIsNull).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.NegativeIndexerType)
                .IsEqualTo(DataType.Void)
                .ConfigureAwait(false);
            await Assert.That(result.First).IsEqualTo(10d).ConfigureAwait(false);
            await Assert.That(result.Second).IsEqualTo(20d).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewTreatsNullStoredArgumentsAsNil()
        {
            NullStoredArgumentResult result = ExecuteNullStoredArgumentView();

            await Assert.That(result.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.RawType).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(result.IndexerType).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(result.CopyType).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [Test]
        [Arguments(false)]
        [Arguments(true)]
        public async Task CallbackArgumentsTreatTupleExpandedNullsAsNil(bool useArgumentView)
        {
            NullStoredArgumentResult result = ExecuteNullTupleExpansion(useArgumentView);

            await Assert.That(result.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.RawType).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(result.IndexerType).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(result.CopyType).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsFalseWhenArrayContainsNull()
        {
            DynValue[] backing = new[] { DynValue.NewNumber(1), null };
            CallbackArguments args = new(backing, false);

            TryGetSpanMetadata legacy = ExecuteTryGetSpanMetadata(args);
            TryGetSpanMetadata view = ExecuteViewTryGetSpanMetadata(backing);

            await Assert.That(legacy.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(legacy.Length).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(view.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(view.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task FastStackTryGetSpanExposesValidRanges()
        {
            FastStack<DynValue> stack = new(4);
            stack.Push(DynValue.NewNumber(1));
            stack.Push(DynValue.NewNumber(2));
            stack.Push(DynValue.NewNumber(3));

            TryGetSpanResult result = ExecuteFastStackTryGetSpan(stack, offset: 1, count: 2);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(3d).ConfigureAwait(false);
        }

        [Test]
        public async Task FastStackTryGetSpanRejectsInvalidRanges()
        {
            FastStack<DynValue> stack = new(2);
            stack.Push(DynValue.NewNumber(1));

            TryGetSpanResult negativeStart = ExecuteFastStackTryGetSpan(
                stack,
                offset: -1,
                count: 1
            );
            TryGetSpanResult negativeLength = ExecuteFastStackTryGetSpan(
                stack,
                offset: 0,
                count: -1
            );
            TryGetSpanResult tooLong = ExecuteFastStackTryGetSpan(stack, offset: 0, count: 2);

            await Assert.That(negativeStart.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(negativeStart.Length).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(negativeLength.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(negativeLength.Length).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(tooLong.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(tooLong.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task FastStackDynamicTryGetSpanAlwaysReturnsFalse()
        {
            FastStackDynamic<DynValue> stack = new(startingCapacity: 2);
            stack.Push(DynValue.NewNumber(1));
            stack.Push(DynValue.NewNumber(2));

            TryGetSpanResult result = ExecuteFastStackDynamicTryGetSpan(stack, offset: 0, count: 2);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsFalseForList()
        {
            // Lists don't expose their backing array, so TryGetSpan returns false
            List<DynValue> backing = new() { DynValue.NewNumber(1), DynValue.NewNumber(2) };
            CallbackArguments args = new(backing, false);

            TryGetSpanResult result = ExecuteTryGetSpan(args);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsFalseWhenTupleExpansionNeeded()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(10), DynValue.NewNumber(20));
            DynValue[] backing = new[] { DynValue.NewNumber(1), tuple };
            CallbackArguments args = new(backing, false);

            // When tuple expansion changes Count from array length, TryGetSpan returns false
            TryGetSpanResult result = ExecuteTryGetSpan(args);

            // Expanded count is 3 (1, 10, 20) but array length is 2
            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanCopiesAllElements()
        {
            DynValue[] backing = new[]
            {
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
            };
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 5);

            await Assert.That(result.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanHandlesSmallDestination()
        {
            DynValue[] backing = new[]
            {
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
            };
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 2);

            await Assert.That(result.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanHandlesEmptyArgs()
        {
            List<DynValue> backing = new();
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 3);

            await Assert.That(result.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanHandlesEmptyDestination()
        {
            DynValue[] backing = new[] { DynValue.NewNumber(1), DynValue.NewNumber(2) };
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 0);

            await Assert.That(result.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanExpandsTuples()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(10), DynValue.NewNumber(20));
            DynValue[] backing = new[] { DynValue.NewNumber(1), tuple };
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 5);

            // Should expand: 1, 10, 20
            await Assert.That(result.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(20).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewCopyToSpanExpandsTuples()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(10), DynValue.NewNumber(20));
            DynValue[] backing = new[] { DynValue.NewNumber(1), tuple };

            CopyToResult result = ExecuteViewCopyTo(backing, 5);

            await Assert.That(result.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(20).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewSkipMethodCallSkipsSelf()
        {
            DynValue[] backing = new[]
            {
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
            };

            CopyToResult result = ExecuteViewSkipMethodCall(backing);

            await Assert.That(result.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanWithSkipCopiesRemaining()
        {
            DynValue[] backing = new[]
            {
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
                DynValue.NewNumber(4),
            };
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 5, 2);

            await Assert.That(result.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(4).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanWithSkipHandlesSkipAll()
        {
            DynValue[] backing = new[] { DynValue.NewNumber(1), DynValue.NewNumber(2) };
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 3, 5);

            await Assert.That(result.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanWithSkipHandlesZeroSkip()
        {
            DynValue[] backing = new[] { DynValue.NewNumber(1), DynValue.NewNumber(2) };
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 3, 0);

            await Assert.That(result.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanWithSkipExpandsTuples()
        {
            DynValue tuple = DynValue.NewTuple(
                DynValue.NewNumber(10),
                DynValue.NewNumber(20),
                DynValue.NewNumber(30)
            );
            DynValue[] backing = new[] { DynValue.NewNumber(1), tuple };
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 5, 2);

            // Expanded: 1, 10, 20, 30 -> skip 2 -> 20, 30
            await Assert.That(result.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(20).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(30).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task SpanAccessWorksWithScriptCallbacks(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            double[] capturedNumbers = null;

            script.Globals["capture"] =
                (Func<CallbackArguments, DynValue>)(
                    args =>
                    {
                        DynValue[] buffer = new DynValue[10];
                        int count = args.CopyTo(buffer.AsSpan());
                        capturedNumbers = new double[count];
                        for (int i = 0; i < count; i++)
                        {
                            capturedNumbers[i] = buffer[i].Number;
                        }
                        return DynValue.Nil;
                    }
                );

            script.DoString("capture(1, 2, 3)");

            await Assert.That(capturedNumbers).IsNotNull().ConfigureAwait(false);
            await Assert.That(capturedNumbers.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(capturedNumbers[0]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(capturedNumbers[1]).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(capturedNumbers[2]).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task CountPropertyReflectsArguments()
        {
            DynValue[] backing = new[]
            {
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3),
            };
            CallbackArguments args = new(backing, false);

            await Assert.That(args.Count).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task CountPropertyExpandsTuples()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(10), DynValue.NewNumber(20));
            DynValue[] backing = new[] { DynValue.NewNumber(1), tuple };
            CallbackArguments args = new(backing, false);

            // 1 + 2 tuple elements = 3
            await Assert.That(args.Count).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task IndexerAccessesExpandedTuples()
        {
            DynValue tuple = DynValue.NewTuple(DynValue.NewNumber(10), DynValue.NewNumber(20));
            DynValue[] backing = new[] { DynValue.NewNumber(1), tuple };
            CallbackArguments args = new(backing, false);

            await Assert.That(args[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(args[1].Number).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(args[2].Number).IsEqualTo(20).ConfigureAwait(false);
        }

        [Test]
        public async Task IsMethodCallPropertyReturnsCorrectValue()
        {
            DynValue[] backing = new[] { DynValue.NewNumber(1) };

            CallbackArguments nonMethodCall = new(backing, false);
            CallbackArguments methodCall = new(backing, true);

            await Assert.That(nonMethodCall.IsMethodCall).IsFalse().ConfigureAwait(false);
            await Assert.That(methodCall.IsMethodCall).IsTrue().ConfigureAwait(false);
        }
    }
}
