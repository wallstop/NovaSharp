namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NovaSharp;
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
            public bool NegativeTryRawGetSucceeded { get; }
            public DataType NegativeTryRawGetType { get; }
            public DataType NegativeIndexerType { get; }
            public double First { get; }
            public double Second { get; }

            public SubrangeResult(
                bool negativeRawGetIsNull,
                bool negativeTryRawGetSucceeded,
                DataType negativeTryRawGetType,
                DataType negativeIndexerType,
                double first,
                double second
            )
            {
                NegativeRawGetIsNull = negativeRawGetIsNull;
                NegativeTryRawGetSucceeded = negativeTryRawGetSucceeded;
                NegativeTryRawGetType = negativeTryRawGetType;
                NegativeIndexerType = negativeIndexerType;
                First = first;
                Second = second;
            }
        }

        private readonly struct PresenceResult
        {
            public bool Found { get; }
            public DataType Type { get; }

            public PresenceResult(bool found, DataType type)
            {
                Found = found;
                Type = type;
            }
        }

        private readonly struct FixedPresenceResult
        {
            public int Count { get; }
            public bool AllStoredArgumentsFound { get; }
            public bool MissingArgumentFound { get; }
            public DataType MissingArgumentType { get; }

            public FixedPresenceResult(
                int count,
                bool allStoredArgumentsFound,
                bool missingArgumentFound,
                DataType missingArgumentType
            )
            {
                Count = count;
                AllStoredArgumentsFound = allStoredArgumentsFound;
                MissingArgumentFound = missingArgumentFound;
                MissingArgumentType = missingArgumentType;
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
            bool result = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
            double[] numbers = new double[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                numbers[i] = span[i].Number;
            }
            return new TryGetSpanResult(result, span.Length, numbers);
        }

        private static TryGetSpanResult ExecuteViewTryGetSpan(LuaValue[] backing)
        {
            CallbackArgumentsView args = new((IList<LuaValue>)backing, false);
            bool result = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
            double[] numbers = new double[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                numbers[i] = span[i].Number;
            }
            return new TryGetSpanResult(result, span.Length, numbers);
        }

        private static TryGetSpanResult ExecuteFastStackTryGetSpan(
            FastStack<LuaValue> stack,
            int offset,
            int count
        )
        {
            bool result = stack.TryGetSpan(offset, count, out ReadOnlySpan<LuaValue> span);
            double[] numbers = new double[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                numbers[i] = span[i].Number;
            }

            return new TryGetSpanResult(result, span.Length, numbers);
        }

        private static TryGetSpanResult ExecuteFastStackDynamicTryGetSpan(
            FastStackDynamic<LuaValue> stack,
            int offset,
            int count
        )
        {
            bool result = stack.TryGetSpan(offset, count, out ReadOnlySpan<LuaValue> span);
            return new TryGetSpanResult(result, span.Length);
        }

        private static TryGetSpanResult ExecuteFastStackSliceTryGetSpan(
            FastStack<LuaValue> stack,
            int offset,
            int count,
            bool reversed = false
        )
        {
            Slice<LuaValue> slice = new(stack, offset, count, reversed);
            CallbackArguments args = new(slice, false);
            return ExecuteTryGetSpan(args);
        }

        private static TryGetSpanResult ExecuteArraySliceTryGetSpan(
            LuaValue[] backing,
            int offset,
            int count,
            bool reversed = false
        )
        {
            Slice<LuaValue> slice = new(backing, offset, count, reversed);
            CallbackArguments args = new(slice, false);
            return ExecuteTryGetSpan(args);
        }

        private static TryGetSpanResult ExecuteListSliceTryGetSpan(List<LuaValue> backing)
        {
            Slice<LuaValue> slice = new(backing, 0, backing.Count, false);
            CallbackArguments args = new(slice, false);
            return ExecuteTryGetSpan(args);
        }

        private static TryGetSpanResult ExecuteFastStackDynamicSliceTryGetSpan(
            FastStackDynamic<LuaValue> backing
        )
        {
            Slice<LuaValue> slice = new(backing, 0, backing.Count, false);
            CallbackArguments args = new(slice, false);
            return ExecuteTryGetSpan(args);
        }

        private static TryGetSpanResult ExecuteViewSliceTryGetSpan(
            IList<LuaValue> backing,
            int offset,
            int count,
            bool reversed = false
        )
        {
            Slice<LuaValue> slice = new(backing, offset, count, reversed);
            CallbackArgumentsView args = new(slice, false);
            bool result = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
            double[] numbers = new double[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                numbers[i] = span[i].Number;
            }

            return new TryGetSpanResult(result, span.Length, numbers);
        }

        private static TryGetSpanResult ExecuteViewSliceSubrangeTryGetSpan(
            IList<LuaValue> backing,
            int sliceOffset,
            int sliceCount,
            int viewOffset,
            int viewCount
        )
        {
            Slice<LuaValue> slice = new(backing, sliceOffset, sliceCount, false);
            CallbackArgumentsView args = new(
                slice,
                offset: viewOffset,
                count: viewCount,
                isMethodCall: false
            );
            bool result = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
            double[] numbers = new double[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                numbers[i] = span[i].Number;
            }

            return new TryGetSpanResult(result, span.Length, numbers);
        }

        private static TryGetSpanResult ExecuteViewSliceSkipMethodCallTryGetSpan(
            IList<LuaValue> backing,
            int offset,
            int count
        )
        {
            Slice<LuaValue> slice = new(backing, offset, count, false);
            CallbackArgumentsView args = new(slice, isMethodCall: true);
            CallbackArgumentsView skipped = args.SkipMethodCall();
            bool result = skipped.TryGetSpan(out ReadOnlySpan<LuaValue> span);
            double[] numbers = new double[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                numbers[i] = span[i].Number;
            }

            return new TryGetSpanResult(result, span.Length, numbers);
        }

        private static TryGetSpanResult ExecuteSkippedFastStackSliceTryGetSpan(
            FastStack<LuaValue> stack
        )
        {
            Slice<LuaValue> slice = new(stack, 0, stack.Count, false);
            CallbackArguments args = new(slice, true);
            return ExecuteTryGetSpan(args.SkipMethodCall());
        }

        private static TryGetSpanResult ExecuteLuaLegacyTryGetSpan(
            LuaCompatibilityVersion version,
            string chunk
        )
        {
            Script script = new(version);
            TryGetSpanResult captured = default;

            script.Globals["capture"] =
                (Func<CallbackArguments, LuaValue>)(
                    args =>
                    {
                        captured = ExecuteTryGetSpan(args);
                        return LuaValue.Nil;
                    }
                );

            script.DoString(chunk);
            return captured;
        }

        private static SubrangeResult ExecuteViewSubrange(FastStackDynamic<LuaValue> backing)
        {
            CallbackArgumentsView args = new(backing, offset: 1, count: 2, isMethodCall: false);
            bool found = args.TryRawGet(-1, translateVoids: true, out LuaValue missing);
            return new SubrangeResult(
                args.RawGet(-1, translateVoids: true) == null,
                found,
                missing.Type,
                args[-1].Type,
                args[0].Number,
                args[1].Number
            );
        }

        private static PresenceResult ExecuteTryRawGet(
            CallbackArguments args,
            int index,
            bool translateVoids
        )
        {
            bool found = args.TryRawGet(index, translateVoids, out LuaValue value);
            return new PresenceResult(found, value.Type);
        }

        private static PresenceResult ExecuteViewTryRawGet(
            IList<LuaValue> backing,
            int index,
            bool translateVoids
        )
        {
            CallbackArgumentsView args = new(backing, isMethodCall: false);
            bool found = args.TryRawGet(index, translateVoids, out LuaValue value);
            return new PresenceResult(found, value.Type);
        }

        private static FixedPresenceResult ExecuteFixedTryRawGet(int count, bool useArgumentView)
        {
            LuaValue one = LuaValue.NewNumber(1);
            LuaValue two = LuaValue.NewNumber(2);
            LuaValue three = LuaValue.NewNumber(3);
            LuaValue four = LuaValue.NewNumber(4);
            LuaValue five = LuaValue.NewNumber(5);
            LuaValue six = LuaValue.NewNumber(6);
            LuaValue seven = LuaValue.NewNumber(7);

            if (useArgumentView)
            {
                CallbackArgumentsView args = count switch
                {
                    0 => new CallbackArgumentsView(isMethodCall: false),
                    1 => new CallbackArgumentsView(one, isMethodCall: false),
                    2 => new CallbackArgumentsView(one, two, isMethodCall: false),
                    3 => new CallbackArgumentsView(one, two, three, isMethodCall: false),
                    4 => new CallbackArgumentsView(one, two, three, four, isMethodCall: false),
                    5 => new CallbackArgumentsView(
                        one,
                        two,
                        three,
                        four,
                        five,
                        isMethodCall: false
                    ),
                    6 => new CallbackArgumentsView(
                        one,
                        two,
                        three,
                        four,
                        five,
                        six,
                        isMethodCall: false
                    ),
                    7 => new CallbackArgumentsView(
                        one,
                        two,
                        three,
                        four,
                        five,
                        six,
                        seven,
                        isMethodCall: false
                    ),
                    _ => throw new ArgumentOutOfRangeException(nameof(count)),
                };

                bool allStoredArgumentsFound = true;
                for (int i = 0; i < count; i++)
                {
                    allStoredArgumentsFound &= args.TryRawGet(
                        i,
                        translateVoids: false,
                        out LuaValue _
                    );
                }

                bool missingArgumentFound = args.TryRawGet(
                    count,
                    translateVoids: false,
                    out LuaValue missingArgument
                );
                return new FixedPresenceResult(
                    args.Count,
                    allStoredArgumentsFound,
                    missingArgumentFound,
                    missingArgument.Type
                );
            }

            CallbackArguments legacyArgs = count switch
            {
                0 => new CallbackArguments(isMethodCall: false),
                1 => new CallbackArguments(one, isMethodCall: false),
                2 => new CallbackArguments(one, two, isMethodCall: false),
                3 => new CallbackArguments(one, two, three, isMethodCall: false),
                4 => new CallbackArguments(one, two, three, four, isMethodCall: false),
                5 => new CallbackArguments(one, two, three, four, five, isMethodCall: false),
                6 => new CallbackArguments(one, two, three, four, five, six, isMethodCall: false),
                7 => new CallbackArguments(
                    one,
                    two,
                    three,
                    four,
                    five,
                    six,
                    seven,
                    isMethodCall: false
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(count)),
            };

            bool allLegacyStoredArgumentsFound = true;
            for (int i = 0; i < count; i++)
            {
                allLegacyStoredArgumentsFound &= legacyArgs.TryRawGet(
                    i,
                    translateVoids: false,
                    out LuaValue _
                );
            }

            bool legacyMissingArgumentFound = legacyArgs.TryRawGet(
                count,
                translateVoids: false,
                out LuaValue legacyMissingArgument
            );
            return new FixedPresenceResult(
                legacyArgs.Count,
                allLegacyStoredArgumentsFound,
                legacyMissingArgumentFound,
                legacyMissingArgument.Type
            );
        }

        private static DataType[] ExecutePooledTupleExpansion()
        {
            CallbackArguments args = new(
                new[] { LuaValue.NewNumber(1), LuaValue.NewTuple(LuaValue.Void, LuaValue.Nil) },
                isMethodCall: false
            );

            using PooledResource<LuaValue[]> pooled = args.GetPooledArray(out LuaValue[] values);
            DataType[] types = new DataType[args.Count];
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = values[i].Type;
            }

            return types;
        }

        private static NullStoredArgumentResult ExecuteNullStoredFixedArgument(bool useArgumentView)
        {
            if (useArgumentView)
            {
                CallbackArgumentsView args = new(
                    LuaValue.NewNumber(1),
                    default,
                    isMethodCall: false
                );
                LuaValue? raw = args.RawGet(1, translateVoids: false);
                LuaValue value = args[1];
                LuaValue[] buffer = new LuaValue[args.Count];
                args.CopyTo(buffer);

                return new NullStoredArgumentResult(
                    args.Count,
                    raw.Value.Type,
                    value.Type,
                    buffer[1].Type
                );
            }

            CallbackArguments legacyArgs = new(LuaValue.NewNumber(1), default, isMethodCall: false);
            LuaValue? legacyRaw = legacyArgs.RawGet(1, translateVoids: false);
            LuaValue legacyValue = legacyArgs[1];
            LuaValue[] legacyBuffer = new LuaValue[legacyArgs.Count];
            legacyArgs.CopyTo(legacyBuffer);

            return new NullStoredArgumentResult(
                legacyArgs.Count,
                legacyRaw.Value.Type,
                legacyValue.Type,
                legacyBuffer[1].Type
            );
        }

        private static NullStoredArgumentResult ExecuteNullTupleExpansion(bool useArgumentView)
        {
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewNumber(10), default);
            LuaValue[] backing = new[] { LuaValue.NewNumber(1), tuple };

            if (useArgumentView)
            {
                CallbackArgumentsView args = new((IList<LuaValue>)backing, false);
                LuaValue? raw = args.RawGet(2, translateVoids: false);
                LuaValue value = args[2];
                LuaValue[] buffer = new LuaValue[args.Count];
                args.CopyTo(buffer);

                return new NullStoredArgumentResult(
                    args.Count,
                    raw.Value.Type,
                    value.Type,
                    buffer[2].Type
                );
            }
            else
            {
                CallbackArguments args = new(backing, false);
                LuaValue? raw = args.RawGet(2, translateVoids: false);
                LuaValue value = args[2];
                LuaValue[] buffer = new LuaValue[args.Count];
                args.CopyTo(buffer);

                return new NullStoredArgumentResult(
                    args.Count,
                    raw.Value.Type,
                    value.Type,
                    buffer[2].Type
                );
            }
        }

        private static TryGetSpanMetadata ExecuteTryGetSpanMetadata(CallbackArguments args)
        {
            bool success = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
            return new TryGetSpanMetadata(success, span.Length);
        }

        private static TryGetSpanMetadata ExecuteViewTryGetSpanMetadata(LuaValue[] backing)
        {
            CallbackArgumentsView args = new((IList<LuaValue>)backing, false);
            bool success = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
            return new TryGetSpanMetadata(success, span.Length);
        }

        private static DataType GetArgumentViewTypeAtZero(LuaValue[] backing)
        {
            CallbackArgumentsView args = new((IList<LuaValue>)backing, false);
            return args[0].Type;
        }

        private static LuaValue CreateArrayValueRequiringNormalization(string valueKind)
        {
            switch (valueKind)
            {
                case "null":
                    return default;
                case "void":
                    return LuaValue.Void;
                case "tuple":
                    return LuaValue.NewTuple(LuaValue.NewNumber(10), LuaValue.NewNumber(11));
                default:
                    throw new ArgumentOutOfRangeException(nameof(valueKind));
            }
        }

        private static CopyToResult ExecuteCopyTo(
            CallbackArguments args,
            int bufferSize,
            int skip = 0
        )
        {
            LuaValue[] buffer = new LuaValue[bufferSize];
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
            IList<LuaValue> backing,
            int bufferSize,
            int skip = 0
        )
        {
            CallbackArgumentsView args = new(backing, false);
            LuaValue[] buffer = new LuaValue[bufferSize];
            int count =
                skip == 0 ? args.CopyTo(buffer.AsSpan()) : args.CopyTo(buffer.AsSpan(), skip);
            double[] numbers = new double[count];
            for (int i = 0; i < count; i++)
            {
                numbers[i] = buffer[i].Number;
            }
            return new CopyToResult(count, numbers);
        }

        private static CopyToResult ExecuteViewSkipMethodCall(LuaValue[] backing)
        {
            CallbackArgumentsView args = new((IList<LuaValue>)backing, true);
            CallbackArgumentsView skipped = args.SkipMethodCall();
            LuaValue[] buffer = new LuaValue[skipped.Count];
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
            List<LuaValue> args = new();
            CallbackArguments callbackArgs = new(args, false);

            TryGetSpanResult result = ExecuteTryGetSpan(callbackArgs);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsTrueForArray()
        {
            LuaValue[] backing = new[]
            {
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
            };
            CallbackArguments args = new(backing, false);

            TryGetSpanResult result = ExecuteTryGetSpan(args);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(3).ConfigureAwait(false);

            LuaValue[] defaultNilBacking =
            {
                LuaValue.NewNumber(1),
                default,
                LuaValue.NewNumber(3),
            };
            TryGetSpanMetadata defaultNilLegacy = ExecuteTryGetSpanMetadata(
                new CallbackArguments(defaultNilBacking, false)
            );
            TryGetSpanMetadata defaultNilView = ExecuteViewTryGetSpanMetadata(defaultNilBacking);
            await Assert.That(defaultNilLegacy.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(defaultNilLegacy.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(defaultNilView.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(defaultNilView.Length).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewTryGetSpanReturnsTrueForArray()
        {
            LuaValue[] backing = new[]
            {
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
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
            FastStackDynamic<LuaValue> backing = new(startingCapacity: 4);
            backing.Push(LuaValue.NewString("function-slot"));
            backing.Push(LuaValue.NewNumber(10));
            backing.Push(LuaValue.NewNumber(20));

            SubrangeResult result = ExecuteViewSubrange(backing);

            await Assert.That(result.NegativeRawGetIsNull).IsTrue().ConfigureAwait(false);
            await Assert.That(result.NegativeTryRawGetSucceeded).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.NegativeTryRawGetType)
                .IsEqualTo(DataType.Void)
                .ConfigureAwait(false);
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
            foreach (bool useArgumentView in new[] { false, true })
            {
                NullStoredArgumentResult result = ExecuteNullStoredFixedArgument(useArgumentView);
                await Assert.That(result.Count).IsEqualTo(2).ConfigureAwait(false);
                await Assert.That(result.RawType).IsEqualTo(DataType.Nil).ConfigureAwait(false);
                await Assert.That(result.IndexerType).IsEqualTo(DataType.Nil).ConfigureAwait(false);
                await Assert.That(result.CopyType).IsEqualTo(DataType.Nil).ConfigureAwait(false);

                LuaValue[] backing = new[] { LuaValue.Nil, LuaValue.Void, LuaValue.NewNumber(3) };
                PresenceResult explicitNil = useArgumentView
                    ? ExecuteViewTryRawGet(backing, 0, translateVoids: false)
                    : ExecuteTryRawGet(
                        new CallbackArguments(backing, false),
                        0,
                        translateVoids: false
                    );
                PresenceResult explicitVoid = useArgumentView
                    ? ExecuteViewTryRawGet(backing, 1, translateVoids: false)
                    : ExecuteTryRawGet(
                        new CallbackArguments(backing, false),
                        1,
                        translateVoids: false
                    );
                PresenceResult translatedVoid = useArgumentView
                    ? ExecuteViewTryRawGet(backing, 1, translateVoids: true)
                    : ExecuteTryRawGet(
                        new CallbackArguments(backing, false),
                        1,
                        translateVoids: true
                    );
                PresenceResult missing = useArgumentView
                    ? ExecuteViewTryRawGet(backing, backing.Length, translateVoids: false)
                    : ExecuteTryRawGet(
                        new CallbackArguments(backing, false),
                        backing.Length,
                        translateVoids: false
                    );

                await Assert.That(explicitNil.Found).IsTrue().ConfigureAwait(false);
                await Assert.That(explicitNil.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
                await Assert.That(explicitVoid.Found).IsTrue().ConfigureAwait(false);
                await Assert.That(explicitVoid.Type).IsEqualTo(DataType.Void).ConfigureAwait(false);
                await Assert.That(translatedVoid.Found).IsTrue().ConfigureAwait(false);
                await Assert
                    .That(translatedVoid.Type)
                    .IsEqualTo(DataType.Nil)
                    .ConfigureAwait(false);
                await Assert.That(missing.Found).IsFalse().ConfigureAwait(false);
                await Assert.That(missing.Type).IsEqualTo(DataType.Void).ConfigureAwait(false);

                for (int count = 0; count <= 7; count++)
                {
                    FixedPresenceResult fixedResult = ExecuteFixedTryRawGet(count, useArgumentView);
                    await Assert.That(fixedResult.Count).IsEqualTo(count).ConfigureAwait(false);
                    await Assert
                        .That(fixedResult.AllStoredArgumentsFound)
                        .IsTrue()
                        .ConfigureAwait(false);
                    await Assert
                        .That(fixedResult.MissingArgumentFound)
                        .IsFalse()
                        .ConfigureAwait(false);
                    await Assert
                        .That(fixedResult.MissingArgumentType)
                        .IsEqualTo(DataType.Void)
                        .ConfigureAwait(false);
                }
            }

            DataType[] pooledTypes = ExecutePooledTupleExpansion();
            await Assert.That(pooledTypes.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(pooledTypes[0]).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(pooledTypes[1]).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(pooledTypes[2]).IsEqualTo(DataType.Nil).ConfigureAwait(false);
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
        [Arguments("void", DataType.Nil)]
        [Arguments("tuple", DataType.Number)]
        public async Task TryGetSpanReturnsFalseWhenArrayValueRequiresNormalization(
            string valueKind,
            DataType indexerType
        )
        {
            LuaValue[] backing = new[]
            {
                CreateArrayValueRequiringNormalization(valueKind),
                LuaValue.NewNumber(2),
            };
            CallbackArguments args = new(backing, false);

            TryGetSpanMetadata legacy = ExecuteTryGetSpanMetadata(args);
            TryGetSpanMetadata viewMetadata = ExecuteViewTryGetSpanMetadata(backing);

            await Assert.That(args[0].Type).IsEqualTo(indexerType).ConfigureAwait(false);
            await Assert
                .That(GetArgumentViewTypeAtZero(backing))
                .IsEqualTo(indexerType)
                .ConfigureAwait(false);
            await Assert.That(legacy.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(legacy.Length).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(viewMetadata.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(viewMetadata.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsTrueWhenTrailingArrayVoidIsTrimmed()
        {
            LuaValue[] backing = new[] { LuaValue.NewNumber(1), LuaValue.Void };
            CallbackArguments args = new(backing, false);

            TryGetSpanResult legacy = ExecuteTryGetSpan(args);
            TryGetSpanResult view = ExecuteViewTryGetSpan(backing);

            await Assert.That(args.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(legacy.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(legacy.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(legacy.Numbers[0]).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(view.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(view.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(view.Numbers[0]).IsEqualTo(1d).ConfigureAwait(false);
        }

        [Test]
        public async Task FastStackTryGetSpanExposesValidRanges()
        {
            FastStack<LuaValue> stack = new(4);
            stack.Push(LuaValue.NewNumber(1));
            stack.Push(LuaValue.NewNumber(2));
            stack.Push(LuaValue.NewNumber(3));

            TryGetSpanResult result = ExecuteFastStackTryGetSpan(stack, offset: 1, count: 2);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(3d).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsTrueForFastStackSlice()
        {
            FastStack<LuaValue> stack = new(4);
            stack.Push(LuaValue.NewNumber(1));
            stack.Push(LuaValue.NewNumber(2));
            stack.Push(LuaValue.NewNumber(3));
            stack.Push(LuaValue.NewNumber(4));

            TryGetSpanResult result = ExecuteFastStackSliceTryGetSpan(stack, offset: 1, count: 3);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(4d).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewTryGetSpanReturnsTrueForFastStackSlice()
        {
            FastStack<LuaValue> stack = new(4);
            stack.Push(LuaValue.NewNumber(1));
            stack.Push(LuaValue.NewNumber(2));
            stack.Push(LuaValue.NewNumber(3));
            stack.Push(LuaValue.NewNumber(4));

            TryGetSpanResult result = ExecuteViewSliceTryGetSpan(stack, offset: 1, count: 3);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(4d).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewTryGetSpanComposesFastStackSliceSubrange()
        {
            FastStack<LuaValue> stack = new(5);
            stack.Push(LuaValue.NewNumber(99));
            stack.Push(LuaValue.NewNumber(1));
            stack.Push(LuaValue.NewNumber(2));
            stack.Push(LuaValue.NewNumber(3));
            stack.Push(LuaValue.NewNumber(100));

            TryGetSpanResult result = ExecuteViewSliceSubrangeTryGetSpan(
                stack,
                sliceOffset: 1,
                sliceCount: 3,
                viewOffset: 1,
                viewCount: 2
            );

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(3d).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewSkipMethodCallPreservesFastStackSliceSpan()
        {
            FastStack<LuaValue> stack = new(5);
            stack.Push(LuaValue.NewNumber(99));
            stack.Push(LuaValue.NewString("self"));
            stack.Push(LuaValue.NewNumber(1));
            stack.Push(LuaValue.NewNumber(2));
            stack.Push(LuaValue.NewNumber(100));

            TryGetSpanResult result = ExecuteViewSliceSkipMethodCallTryGetSpan(
                stack,
                offset: 1,
                count: 3
            );

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(2d).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsTrueForArraySlice()
        {
            LuaValue[] backing =
            {
                LuaValue.NewNumber(99),
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                LuaValue.NewNumber(100),
            };

            TryGetSpanResult result = ExecuteArraySliceTryGetSpan(backing, offset: 1, count: 3);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(3d).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewTryGetSpanReturnsTrueForArraySlice()
        {
            LuaValue[] backing =
            {
                LuaValue.NewNumber(99),
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                LuaValue.NewNumber(100),
            };

            TryGetSpanResult result = ExecuteViewSliceTryGetSpan(backing, offset: 1, count: 3);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(3d).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsTrueWhenTrailingArraySliceVoidIsTrimmed()
        {
            LuaValue[] backing =
            {
                LuaValue.NewNumber(99),
                LuaValue.NewNumber(1),
                LuaValue.Void,
                LuaValue.NewNumber(100),
            };

            TryGetSpanResult result = ExecuteArraySliceTryGetSpan(backing, offset: 1, count: 2);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1d).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewTryGetSpanReturnsTrueWhenTrailingArraySliceVoidIsTrimmed()
        {
            LuaValue[] backing =
            {
                LuaValue.NewNumber(99),
                LuaValue.NewNumber(1),
                LuaValue.Void,
                LuaValue.NewNumber(100),
            };

            TryGetSpanResult result = ExecuteViewSliceTryGetSpan(backing, offset: 1, count: 2);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1d).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsFalseForReversedArraySlice()
        {
            LuaValue[] backing =
            {
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
            };

            TryGetSpanResult result = ExecuteArraySliceTryGetSpan(
                backing,
                offset: 0,
                count: 3,
                reversed: true
            );

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewTryGetSpanReturnsFalseForReversedArraySlice()
        {
            LuaValue[] backing =
            {
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
            };

            TryGetSpanResult result = ExecuteViewSliceTryGetSpan(
                backing,
                offset: 0,
                count: 3,
                reversed: true
            );

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        [Arguments("void")]
        [Arguments("tuple")]
        public async Task TryGetSpanReturnsFalseWhenArraySliceValueRequiresNormalization(
            string valueKind
        )
        {
            LuaValue[] backing =
            {
                LuaValue.NewNumber(99),
                CreateArrayValueRequiringNormalization(valueKind),
                LuaValue.NewNumber(2),
            };

            TryGetSpanResult result = ExecuteArraySliceTryGetSpan(backing, offset: 1, count: 2);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        [Arguments("void")]
        [Arguments("tuple")]
        public async Task ArgumentViewTryGetSpanReturnsFalseWhenArraySliceValueRequiresNormalization(
            string valueKind
        )
        {
            LuaValue[] backing =
            {
                LuaValue.NewNumber(99),
                CreateArrayValueRequiringNormalization(valueKind),
                LuaValue.NewNumber(2),
            };

            TryGetSpanResult result = ExecuteViewSliceTryGetSpan(backing, offset: 1, count: 2);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsTrueForSkippedFastStackSliceMethodCall()
        {
            FastStack<LuaValue> stack = new(4);
            stack.Push(LuaValue.NewString("self"));
            stack.Push(LuaValue.NewNumber(1));
            stack.Push(LuaValue.NewNumber(2));
            stack.Push(LuaValue.NewNumber(3));

            TryGetSpanResult result = ExecuteSkippedFastStackSliceTryGetSpan(stack);

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(3d).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsFalseForReversedFastStackSlice()
        {
            FastStack<LuaValue> stack = new(3);
            stack.Push(LuaValue.NewNumber(1));
            stack.Push(LuaValue.NewNumber(2));
            stack.Push(LuaValue.NewNumber(3));

            TryGetSpanResult result = ExecuteFastStackSliceTryGetSpan(
                stack,
                offset: 0,
                count: 3,
                reversed: true
            );

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        [Arguments("void")]
        [Arguments("tuple")]
        public async Task TryGetSpanReturnsFalseWhenFastStackSliceValueRequiresNormalization(
            string valueKind
        )
        {
            FastStack<LuaValue> stack = new(2);
            stack.Push(CreateArrayValueRequiringNormalization(valueKind));
            stack.Push(LuaValue.NewNumber(2));

            TryGetSpanResult result = ExecuteFastStackSliceTryGetSpan(stack, offset: 0, count: 2);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsFalseWhenFastStackSliceTupleExpansionNeeded()
        {
            FastStack<LuaValue> stack = new(2);
            stack.Push(LuaValue.NewNumber(1));
            stack.Push(LuaValue.NewTuple(LuaValue.NewNumber(10), LuaValue.NewNumber(20)));

            TryGetSpanResult result = ExecuteFastStackSliceTryGetSpan(stack, offset: 0, count: 2);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsFalseForListSlice()
        {
            List<LuaValue> backing = new() { LuaValue.NewNumber(1), LuaValue.NewNumber(2) };

            TryGetSpanResult result = ExecuteListSliceTryGetSpan(backing);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewTryGetSpanReturnsFalseForListSlice()
        {
            List<LuaValue> backing = new() { LuaValue.NewNumber(1), LuaValue.NewNumber(2) };

            TryGetSpanResult result = ExecuteViewSliceTryGetSpan(backing, offset: 0, count: 2);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsFalseForFastStackDynamicSlice()
        {
            FastStackDynamic<LuaValue> backing = new(startingCapacity: 2);
            backing.Push(LuaValue.NewNumber(1));
            backing.Push(LuaValue.NewNumber(2));

            TryGetSpanResult result = ExecuteFastStackDynamicSliceTryGetSpan(backing);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewTryGetSpanReturnsFalseForFastStackDynamicSlice()
        {
            FastStackDynamic<LuaValue> backing = new(startingCapacity: 2);
            backing.Push(LuaValue.NewNumber(1));
            backing.Push(LuaValue.NewNumber(2));

            TryGetSpanResult result = ExecuteViewSliceTryGetSpan(backing, offset: 0, count: 2);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task FastStackTryGetSpanRejectsInvalidRanges()
        {
            FastStack<LuaValue> stack = new(2);
            stack.Push(LuaValue.NewNumber(1));

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
        public async Task FastStackDynamicTryGetSpanReturnsFalseBecauseListBackingIsUnsupported()
        {
            FastStackDynamic<LuaValue> stack = new(startingCapacity: 2);
            stack.Push(LuaValue.NewNumber(1));
            stack.Push(LuaValue.NewNumber(2));

            TryGetSpanResult result = ExecuteFastStackDynamicTryGetSpan(stack, offset: 0, count: 2);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsFalseForList()
        {
            // Lists don't expose their backing array, so TryGetSpan returns false
            List<LuaValue> backing = new() { LuaValue.NewNumber(1), LuaValue.NewNumber(2) };
            CallbackArguments args = new(backing, false);

            TryGetSpanResult result = ExecuteTryGetSpan(args);

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task TryGetSpanReturnsFalseWhenTupleExpansionNeeded()
        {
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewNumber(10), LuaValue.NewNumber(20));
            LuaValue[] backing = new[] { LuaValue.NewNumber(1), tuple };
            CallbackArguments args = new(backing, false);

            // When tuple expansion changes Count from array length, TryGetSpan returns false
            TryGetSpanResult result = ExecuteTryGetSpan(args);

            // Expanded count is 3 (1, 10, 20) but array length is 2
            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanCopiesAllElements()
        {
            LuaValue[] backing = new[]
            {
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
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
            LuaValue[] backing = new[]
            {
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
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
            List<LuaValue> backing = new();
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 3);

            await Assert.That(result.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanHandlesEmptyDestination()
        {
            LuaValue[] backing = new[] { LuaValue.NewNumber(1), LuaValue.NewNumber(2) };
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 0);

            await Assert.That(result.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanExpandsTuples()
        {
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewNumber(10), LuaValue.NewNumber(20));
            LuaValue[] backing = new[] { LuaValue.NewNumber(1), tuple };
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
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewNumber(10), LuaValue.NewNumber(20));
            LuaValue[] backing = new[] { LuaValue.NewNumber(1), tuple };

            CopyToResult result = ExecuteViewCopyTo(backing, 5);

            await Assert.That(result.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(20).ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentViewSkipMethodCallSkipsSelf()
        {
            LuaValue[] backing = new[]
            {
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
            };

            CopyToResult result = ExecuteViewSkipMethodCall(backing);

            await Assert.That(result.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanWithSkipCopiesRemaining()
        {
            LuaValue[] backing = new[]
            {
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                LuaValue.NewNumber(4),
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
            LuaValue[] backing = new[] { LuaValue.NewNumber(1), LuaValue.NewNumber(2) };
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 3, 5);

            await Assert.That(result.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanWithSkipHandlesZeroSkip()
        {
            LuaValue[] backing = new[] { LuaValue.NewNumber(1), LuaValue.NewNumber(2) };
            CallbackArguments args = new(backing, false);

            CopyToResult result = ExecuteCopyTo(args, 3, 0);

            await Assert.That(result.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToSpanWithSkipExpandsTuples()
        {
            LuaValue tuple = LuaValue.NewTuple(
                LuaValue.NewNumber(10),
                LuaValue.NewNumber(20),
                LuaValue.NewNumber(30)
            );
            LuaValue[] backing = new[] { LuaValue.NewNumber(1), tuple };
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
                (Func<CallbackArguments, LuaValue>)(
                    args =>
                    {
                        LuaValue[] buffer = new LuaValue[10];
                        int count = args.CopyTo(buffer.AsSpan());
                        capturedNumbers = new double[count];
                        for (int i = 0; i < count; i++)
                        {
                            capturedNumbers[i] = buffer[i].Number;
                        }
                        return LuaValue.Nil;
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
        [AllLuaVersions]
        public async Task LegacyCallbackArgumentsTryGetSpanSucceedsForVmStackSlice(
            LuaCompatibilityVersion version
        )
        {
            TryGetSpanResult result = ExecuteLuaLegacyTryGetSpan(version, "capture(1, 2, 3)");

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Numbers[0]).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.Numbers[1]).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Numbers[2]).IsEqualTo(3d).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task LegacyCallbackArgumentsTryGetSpanFallsBackWhenTupleExpansionIsNeeded(
            LuaCompatibilityVersion version
        )
        {
            TryGetSpanResult result = ExecuteLuaLegacyTryGetSpan(
                version,
                "local function values() return 2, 3 end capture(1, values())"
            );

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Length).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task CountPropertyReflectsArguments()
        {
            LuaValue[] backing = new[]
            {
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
            };
            CallbackArguments args = new(backing, false);

            await Assert.That(args.Count).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task CountPropertyExpandsTuples()
        {
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewNumber(10), LuaValue.NewNumber(20));
            LuaValue[] backing = new[] { LuaValue.NewNumber(1), tuple };
            CallbackArguments args = new(backing, false);

            // 1 + 2 tuple elements = 3
            await Assert.That(args.Count).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task IndexerAccessesExpandedTuples()
        {
            LuaValue tuple = LuaValue.NewTuple(LuaValue.NewNumber(10), LuaValue.NewNumber(20));
            LuaValue[] backing = new[] { LuaValue.NewNumber(1), tuple };
            CallbackArguments args = new(backing, false);

            await Assert.That(args[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(args[1].Number).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(args[2].Number).IsEqualTo(20).ConfigureAwait(false);
        }

        [Test]
        public async Task IsMethodCallPropertyReturnsCorrectValue()
        {
            LuaValue[] backing = new[] { LuaValue.NewNumber(1) };

            CallbackArguments nonMethodCall = new(backing, false);
            CallbackArguments methodCall = new(backing, true);

            await Assert.That(nonMethodCall.IsMethodCall).IsFalse().ConfigureAwait(false);
            await Assert.That(methodCall.IsMethodCall).IsTrue().ConfigureAwait(false);
        }
    }
}
