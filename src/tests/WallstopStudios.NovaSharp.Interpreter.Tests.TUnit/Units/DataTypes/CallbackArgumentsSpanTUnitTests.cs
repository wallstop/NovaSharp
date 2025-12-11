namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

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
        public async Task SpanAccessWorksWithScriptCallbacks()
        {
            Script script = new();
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
