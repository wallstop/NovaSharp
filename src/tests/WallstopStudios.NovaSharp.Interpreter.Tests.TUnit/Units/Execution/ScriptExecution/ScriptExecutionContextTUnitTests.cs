namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class ScriptExecutionContextTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task EvaluateSymbolByNameResolvesLocals(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue callback = LuaValue.NewCallback(
                (context, _) =>
                {
                    LuaValue local = context.EvaluateSymbolByName("localValue");
                    return local;
                }
            );
            script.Globals["assertLocal"] = callback;

            LuaValue result = script.DoString(
                @"
                function wrapper()
                    local localValue = 123
                    return assertLocal()
                end
                return wrapper()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(123);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CurrentGlobalEnvExposesGlobals(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            script.Globals["marker"] = LuaValue.NewString("available");

            LuaValue callback = LuaValue.NewCallback(
                (context, _) =>
                {
                    Table env = context.CurrentGlobalEnv;
                    LuaValue marker = env.Get("marker");
                    SymbolRef envSymbol = context.FindSymbolByName(WellKnownSymbols.ENV);
                    LuaValue envValue = context.EvaluateSymbol(envSymbol);
                    return LuaValue.NewTuple(marker, envValue);
                }
            );
            script.Globals["probeEnv"] = callback;

            LuaValue tuple = script.DoString(
                @"
                function trigger()
                    return probeEnv()
                end
                return trigger()
            "
            );

            await Assert.That(tuple.Tuple.Length).IsEqualTo(2);
            await Assert.That(tuple.Tuple[0].String).IsEqualTo("available");
            await Assert.That(tuple.Tuple[1].Type).IsEqualTo(DataType.Table);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task GetMetatableReturnsAssignedMetatable(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            LuaValue callback = LuaValue.NewCallback(
                (context, args) =>
                {
                    Table meta = context.GetMetatable(args[0]);
                    return meta?.Get("marker") ?? LuaValue.Nil;
                }
            );
            script.Globals["probeMeta"] = callback;

            LuaValue marker = script.DoString(
                @"
                local t = {}
                setmetatable(t, { marker = 42 })
                return probeMeta(t)
            "
            );

            await Assert.That(marker.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task GetMetamethodTailCallReturnsTailCallRequest(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            LuaValue lastTailCall = LuaValue.Nil;
            LuaValue resolvedMetamethod = LuaValue.Nil;
            bool foundMetamethod = false;
            LuaValue callback = LuaValue.NewCallback(
                (context, args) =>
                {
                    foundMetamethod = context.TryGetMetamethod(
                        args[0],
                        "__call",
                        out resolvedMetamethod
                    );
                    lastTailCall = context.GetMetamethodTailCall(
                        args[0],
                        "__call",
                        args[0],
                        args[1]
                    );
                    return LuaValue.NewNumber(0);
                }
            );
            script.Globals["probeTailCall"] = callback;

            script.DoString(
                @"
                local target = {}
                setmetatable(target, { __call = function(_, value) return value end })
                return probeTailCall(target, 7)
            "
            );

            LuaValue tail = lastTailCall;
            await Assert.That(foundMetamethod).IsTrue();
            await Assert.That(resolvedMetamethod.Type).IsEqualTo(DataType.Function);
            await Assert.That(tail.Type).IsEqualTo(DataType.TailCallRequest);
            await Assert.That(tail.TailCallData.Function.Type).IsEqualTo(DataType.Function);
            await Assert.That(tail.TailCallData.Args.Span[1].Number).IsEqualTo(7);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task PerformMessageDecorationDecoratesException(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            script.DoString(
                @"
                function decorator(message)
                    return 'decorated:' .. message
                end
            "
            );

            LuaValue callback = LuaValue.NewCallback(
                (context, _) =>
                {
                    ScriptRuntimeException exception = new("boom");
                    LuaValue handler = context.Script.Globals.Get("decorator");
                    context.PerformMessageDecorationBeforeUnwind(handler, exception);
                    return LuaValue.NewString(exception.DecoratedMessage);
                }
            );
            script.Globals["decorateMessage"] = callback;

            LuaValue result = script.DoString("return decorateMessage()");
            await Assert.That(result.String).IsEqualTo("decorated:boom");
        }

        [global::TUnit.Core.Test]
        public async Task AdditionalDataRequiresCallback()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                context.AdditionalData = "payload"
            );

            await Assert.That(exception.Message).Contains("no callback");
        }

        [global::TUnit.Core.Test]
        public async Task AdditionalDataFlowsThroughCallback()
        {
            Script script = new(default(CoreModules));
            CallbackFunction callback = new((_, _) => LuaValue.Nil);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext(callback);

            context.AdditionalData = 123;

            await Assert.That(context.AdditionalData).IsEqualTo(123);
            await Assert.That(callback.AdditionalData).IsEqualTo(123);
        }

        [global::TUnit.Core.Test]
        public async Task CallThrowsWhenClrFunctionYields()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue func = LuaValue.NewCallback(
                (_, _) => LuaValue.NewYieldReq(Array.Empty<LuaValue>())
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(func)
            );

            await Assert.That(exception.Message).Contains("yield across a CLR-call boundary");
        }

        [global::TUnit.Core.Test]
        public async Task CallThrowsWhenTailCallHasContinuation()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue func = LuaValue.NewCallback(
                (_, _) =>
                    LuaValue.NewTailCallReq(
                        new TailCallData
                        {
                            Function = LuaValue.NewCallback((_, _) => LuaValue.NewNumber(1)),
                            Continuation = new CallbackFunction((_, _) => LuaValue.Nil),
                        }
                    )
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(func)
            );
            await Assert.That(exception.Message).Contains("cannot be called directly");
        }

        [global::TUnit.Core.Test]
        public async Task CallFollowsTailCallWithoutContinuation()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue inner = LuaValue.NewCallback(
                (_, args) => LuaValue.NewNumber(args[0].Number + 24)
            );
            LuaValue func = LuaValue.NewCallback(
                (_, _) =>
                    LuaValue.NewTailCallReq(
                        new TailCallData
                        {
                            Function = inner,
                            Args = new[] { LuaValue.NewNumber(18) },
                        }
                    )
            );

            LuaValue result = context.Call(func);
            await Assert.That(result.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallInvokesZeroArgumentLuaFunction(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue function = script.DoString("return function() return 42 end");

            LuaValue result = context.Call(function);

            await Assert.That(result.Number).IsEqualTo(42d);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FixedCallOverloadsInvokeLuaFunctions(LuaCompatibilityVersion version)
        {
            for (int arity = 5; arity <= 7; arity++)
            {
                Script script = CreateScript(version);
                LuaValue sum = script.DoString(
                    @"return function(...)
                        local total = 0
                        for i = 1, select('#', ...) do
                            total = total + select(i, ...)
                        end
                        return total
                    end"
                );
                LuaValue[] values = CreateSequentialArguments(arity);
                LuaValue callback = LuaValue.NewCallbackView(
                    (context, _) => CallWithFixedArguments(context, sum, values)
                );
                script.Globals["callSum"] = callback;

                LuaValue result = script.DoString("return callSum()");
                await Assert.That(result.Number).IsEqualTo(arity * (arity + 1) / 2d);
            }
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FixedCallOverloadsInvokeCallbackViews(LuaCompatibilityVersion version)
        {
            for (int arity = 5; arity <= 7; arity++)
            {
                Script script = CreateScript(version);
                LuaValue inner = LuaValue.NewCallbackView(
                    (_, args) =>
                    {
                        double sum = args.Count;
                        for (int i = 0; i < args.Count; i++)
                        {
                            sum += args[i].Number;
                        }

                        return LuaValue.NewNumber(sum);
                    }
                );
                LuaValue[] values = CreateSequentialArguments(arity);
                LuaValue callback = LuaValue.NewCallbackView(
                    (context, _) => CallWithFixedArguments(context, inner, values)
                );
                script.Globals["callInner"] = callback;

                LuaValue result = script.DoString("return callInner()");
                await Assert.That(result.Number).IsEqualTo(arity + arity * (arity + 1) / 2d);
            }
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FixedCallOverloadsPreserveLegacyCallbackArgumentsWithFixedStorageSpan(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            LuaValue inner = LuaValue.NewCallback(
                (_, args) =>
                {
                    bool success = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    LuaValue[] copied = new LuaValue[args.Count];
                    int copiedCount = args.CopyTo(copied);
                    return LuaValue.NewTuple(
                        LuaValue.NewBoolean(success),
                        LuaValue.NewNumber(span.Length),
                        LuaValue.NewNumber(copiedCount),
                        copied[0],
                        copied[1],
                        copied[2]
                    );
                }
            );
            LuaValue callback = LuaValue.NewCallbackView(
                (context, _) =>
                    context.Call(
                        inner,
                        LuaValue.NewNumber(10),
                        LuaValue.NewNumber(20),
                        LuaValue.NewNumber(30)
                    )
            );
            script.Globals["callInner"] = callback;

            LuaValue result = script.DoString("return callInner()");
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Number).IsEqualTo(3d);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(3d);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(10d);
            await Assert.That(result.Tuple[4].Number).IsEqualTo(20d);
            await Assert.That(result.Tuple[5].Number).IsEqualTo(30d);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallWithReadOnlySpanDynValuesPreservesCallbackAdjustmentSemantics(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue inspect = LuaValue.NewCallback((_, args) => SummarizeArguments(args));
            LuaValue[] values =
            {
                LuaValue.NewNumber(1),
                LuaValue.Nil,
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                LuaValue.NewNumber(3),
                LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.Nil),
            };

            LuaValue result = context.Call(inspect, values.AsSpan());

            await AssertArgumentSummary(result, count: 6d, nilCount: 2d, sum: 10d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0)]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(2)]
        [global::TUnit.Core.Arguments(3)]
        [global::TUnit.Core.Arguments(4)]
        [global::TUnit.Core.Arguments(5)]
        [global::TUnit.Core.Arguments(6)]
        [global::TUnit.Core.Arguments(7)]
        public async Task CallWithReadOnlySpanDynValuesExposesSpanToCallbackView(int arity)
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    bool spanAvailable = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    double sum = 0d;
                    for (int i = 0; i < span.Length; i++)
                    {
                        sum += span[i].Number;
                    }

                    return LuaValue.NewTuple(
                        LuaValue.NewBoolean(spanAvailable),
                        LuaValue.NewNumber(span.Length),
                        LuaValue.NewNumber(args.Count),
                        LuaValue.NewNumber(sum)
                    );
                }
            );
            LuaValue[] values = CreateSequentialArguments(arity);

            LuaValue result = context.Call(callback, values.AsSpan());

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo((double)arity)
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].Number)
                .IsEqualTo((double)arity)
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[3].Number)
                .IsEqualTo(arity * (arity + 1) / 2d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallWithReadOnlySpanDynValuesUsesCallMetamethod(
            LuaCompatibilityVersion version
        )
        {
            for (int arity = 5; arity <= 6; arity++)
            {
                Script script = CreateScript(version);
                ScriptExecutionContext context = script.CreateDynamicExecutionContext();
                Table target = new(script);
                Table meta = new(script);
                LuaValue[] values = CreateSequentialArguments(arity);
                meta.Set(
                    "__call",
                    LuaValue.NewCallback(
                        (_, args) =>
                        {
                            double sum = 0d;
                            for (int i = 1; i < args.Count; i++)
                            {
                                sum += args[i].Number;
                            }

                            return LuaValue.NewTuple(
                                LuaValue.NewNumber(args.Count),
                                LuaValue.NewBoolean(ReferenceEquals(args[0].Table, target)),
                                LuaValue.NewNumber(sum)
                            );
                        }
                    )
                );
                target.MetaTable = meta;

                LuaValue result = context.Call(LuaValue.NewTable(target), values.AsSpan());

                await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
                await Assert
                    .That(result.Tuple[0].Number)
                    .IsEqualTo(arity + 1d)
                    .ConfigureAwait(false);
                await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
                await Assert
                    .That(result.Tuple[2].Number)
                    .IsEqualTo(arity * (arity + 1) / 2d)
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task CallWithReadOnlySpanDynValuesRejectsYieldRequest()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue func = LuaValue.NewCallbackView(
                (_, _) => LuaValue.NewYieldReq(Array.Empty<LuaValue>())
            );
            LuaValue[] values = CreateSequentialArguments(5);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(func, values.AsSpan())
            );

            await Assert
                .That(exception.Message)
                .Contains("yield across a CLR-call boundary")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CallWithReadOnlySpanDynValuesRejectsTailCallWithContinuation()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue func = LuaValue.NewCallbackView(
                (_, _) =>
                    LuaValue.NewTailCallReq(
                        new TailCallData
                        {
                            Function = LuaValue.NewCallback((_, _) => LuaValue.NewNumber(1)),
                            Continuation = new CallbackFunction((_, _) => LuaValue.Nil),
                        }
                    )
            );
            LuaValue[] values = CreateSequentialArguments(5);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(func, values.AsSpan())
            );
            await Assert
                .That(exception.Message)
                .Contains("cannot be called directly")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedCallOverloadsAvoidLegacyCallbackArgumentArrayAllocation()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue noArgCallback = LuaValue.NewCallback(
                (_, args) =>
                {
                    if (args.Count != 0)
                    {
                        throw new InvalidOperationException(
                            "No-argument allocation probe received arguments."
                        );
                    }

                    return LuaValue.Nil;
                }
            );
            LuaValue fiveArgCallback = LuaValue.NewCallback(
                (_, args) =>
                {
                    if (
                        args.Count != 5
                        || args[0].Number != 1d
                        || args[1].Number != 2d
                        || args[2].Number != 3d
                        || args[3].Number != 4d
                        || args[4].Number != 5d
                    )
                    {
                        throw new InvalidOperationException(
                            "Fixed five-argument allocation probe received unexpected arguments."
                        );
                    }

                    return LuaValue.Nil;
                }
            );
            LuaValue sixArgCallback = LuaValue.NewCallback(
                (_, args) =>
                {
                    if (
                        args.Count != 6
                        || args[0].Number != 1d
                        || args[1].Number != 2d
                        || args[2].Number != 3d
                        || args[3].Number != 4d
                        || args[4].Number != 5d
                        || args[5].Number != 6d
                    )
                    {
                        throw new InvalidOperationException(
                            "Fixed six-argument allocation probe received unexpected arguments."
                        );
                    }

                    return LuaValue.Nil;
                }
            );
            LuaValue spanProbeCallback = LuaValue.NewCallback(
                (_, args) =>
                {
                    bool hasSpan = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    if (!hasSpan || span.Length != 7)
                    {
                        throw new InvalidOperationException(
                            "Fixed seven-argument allocation probe did not expose the expected span."
                        );
                    }

                    return LuaValue.Nil;
                }
            );
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewNumber(2);
            LuaValue third = LuaValue.NewNumber(3);
            LuaValue fourth = LuaValue.NewNumber(4);
            LuaValue fifth = LuaValue.NewNumber(5);
            LuaValue sixth = LuaValue.NewNumber(6);
            LuaValue seventh = LuaValue.NewNumber(7);

            MeasureNoArgumentContextCallAllocations(context, noArgCallback, iterations: 8);
            MeasureFixedFiveArgumentContextCallAllocations(
                context,
                fiveArgCallback,
                first,
                second,
                third,
                fourth,
                fifth,
                iterations: 8
            );
            MeasureFixedSixArgumentContextCallAllocations(
                context,
                sixArgCallback,
                first,
                second,
                third,
                fourth,
                fifth,
                sixth,
                iterations: 8
            );
            MeasureFixedSevenArgumentContextCallAllocations(
                context,
                spanProbeCallback,
                first,
                second,
                third,
                fourth,
                fifth,
                sixth,
                seventh,
                iterations: 8
            );

            int iterations = 1_024;
            long noArgumentAllocated = MeasureNoArgumentContextCallAllocations(
                context,
                noArgCallback,
                iterations
            );
            long fixedArgumentAllocated = MeasureFixedFiveArgumentContextCallAllocations(
                context,
                fiveArgCallback,
                first,
                second,
                third,
                fourth,
                fifth,
                iterations
            );
            long sixArgumentAllocated = MeasureFixedSixArgumentContextCallAllocations(
                context,
                sixArgCallback,
                first,
                second,
                third,
                fourth,
                fifth,
                sixth,
                iterations
            );
            long spanProbeAllocated = MeasureFixedSevenArgumentContextCallAllocations(
                context,
                spanProbeCallback,
                first,
                second,
                third,
                fourth,
                fifth,
                sixth,
                seventh,
                iterations
            );
            long extraBytesPerCall = (fixedArgumentAllocated - noArgumentAllocated) / iterations;
            long sixArgumentExtraBytesPerCall =
                (sixArgumentAllocated - noArgumentAllocated) / iterations;
            long spanProbeExtraBytesPerCall =
                (spanProbeAllocated - noArgumentAllocated) / iterations;

            await Assert
                .That(extraBytesPerCall)
                .IsLessThan(16L)
                .Because(
                    $"No-argument context calls allocated {noArgumentAllocated} bytes; five fixed-argument context calls allocated {fixedArgumentAllocated} bytes."
                );
            await Assert
                .That(sixArgumentExtraBytesPerCall)
                .IsLessThan(16L)
                .Because(
                    $"No-argument context calls allocated {noArgumentAllocated} bytes; six fixed-argument context calls allocated {sixArgumentAllocated} bytes."
                );
            await Assert
                .That(spanProbeExtraBytesPerCall)
                .IsLessThan(16L)
                .Because(
                    $"No-argument context calls allocated {noArgumentAllocated} bytes; seven fixed-argument span-probe context calls allocated {spanProbeAllocated} bytes."
                );
        }

        [global::TUnit.Core.Test]
        public async Task FixedCallOverloadsAvoidCallMetamethodArgumentArrayAllocation()
        {
            const int iterations = 1_024;
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table callable = new(script);
            Table meta = new(script);
            LuaValue callableValue = LuaValue.NewTable(callable);
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewNumber(2);
            LuaValue third = LuaValue.NewNumber(3);
            LuaValue fourth = LuaValue.NewNumber(4);
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    if (
                        args.Count != 5
                        || !ReferenceEquals(args[0].Table, callable)
                        || args[1].Number != 1d
                        || args[2].Number != 2d
                        || args[3].Number != 3d
                        || args[4].Number != 4d
                    )
                    {
                        throw new InvalidOperationException(
                            "Context metamethod allocation probe received unexpected arguments."
                        );
                    }

                    return LuaValue.Nil;
                }
            );
            meta.Set("__call", callback);
            callable.MetaTable = meta;

            MeasureFixedFiveArgumentContextCallAllocations(
                context,
                callback,
                callableValue,
                first,
                second,
                third,
                fourth,
                iterations: 8
            );
            MeasureFixedFourArgumentContextCallMetamethodAllocations(
                context,
                callableValue,
                first,
                second,
                third,
                fourth,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long directAllocated = MeasureFixedFiveArgumentContextCallAllocations(
                context,
                callback,
                callableValue,
                first,
                second,
                third,
                fourth,
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long metamethodAllocated = MeasureFixedFourArgumentContextCallMetamethodAllocations(
                context,
                callableValue,
                first,
                second,
                third,
                fourth,
                iterations
            );
            long extraBytesPerCall = (metamethodAllocated - directAllocated) / iterations;

            await Assert.That(extraBytesPerCall).IsLessThan(16L);
        }

        [global::TUnit.Core.Test]
        public async Task FixedCallOverloadsAvoidHighArityCallMetamethodArgumentArrayAllocation()
        {
            const int iterations = 1_024;
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table callable = new(script);
            Table meta = new(script);
            LuaValue callableValue = LuaValue.NewTable(callable);
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewNumber(2);
            LuaValue third = LuaValue.NewNumber(3);
            LuaValue fourth = LuaValue.NewNumber(4);
            LuaValue fifth = LuaValue.NewNumber(5);
            LuaValue sixth = LuaValue.NewNumber(6);
            LuaValue fourArgumentCallback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    if (
                        args.Count != 5
                        || !ReferenceEquals(args[0].Table, callable)
                        || args[1].Number != 1d
                        || args[2].Number != 2d
                        || args[3].Number != 3d
                        || args[4].Number != 4d
                    )
                    {
                        throw new InvalidOperationException(
                            "Context four-argument metamethod allocation probe received unexpected arguments."
                        );
                    }

                    return LuaValue.Nil;
                }
            );
            LuaValue fiveArgumentCallback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    if (
                        args.Count != 6
                        || !ReferenceEquals(args[0].Table, callable)
                        || args[1].Number != 1d
                        || args[2].Number != 2d
                        || args[3].Number != 3d
                        || args[4].Number != 4d
                        || args[5].Number != 5d
                    )
                    {
                        throw new InvalidOperationException(
                            "Context five-argument metamethod allocation probe received unexpected arguments."
                        );
                    }

                    return LuaValue.Nil;
                }
            );
            LuaValue sixArgumentCallback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    if (
                        args.Count != 7
                        || !ReferenceEquals(args[0].Table, callable)
                        || args[1].Number != 1d
                        || args[2].Number != 2d
                        || args[3].Number != 3d
                        || args[4].Number != 4d
                        || args[5].Number != 5d
                        || args[6].Number != 6d
                    )
                    {
                        throw new InvalidOperationException(
                            "Context six-argument metamethod allocation probe received unexpected arguments."
                        );
                    }

                    return LuaValue.Nil;
                }
            );
            callable.MetaTable = meta;

            meta.Set("__call", fourArgumentCallback);
            MeasureFixedFourArgumentContextCallMetamethodAllocations(
                context,
                callableValue,
                first,
                second,
                third,
                fourth,
                iterations: 8
            );
            meta.Set("__call", fiveArgumentCallback);
            MeasureFixedFiveArgumentContextCallMetamethodAllocations(
                context,
                callableValue,
                first,
                second,
                third,
                fourth,
                fifth,
                iterations: 8
            );
            meta.Set("__call", sixArgumentCallback);
            MeasureFixedSixArgumentContextCallMetamethodAllocations(
                context,
                callableValue,
                first,
                second,
                third,
                fourth,
                fifth,
                sixth,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            meta.Set("__call", fourArgumentCallback);
            long fourArgumentAllocated = MeasureFixedFourArgumentContextCallMetamethodAllocations(
                context,
                callableValue,
                first,
                second,
                third,
                fourth,
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            meta.Set("__call", fiveArgumentCallback);
            long fiveArgumentAllocated = MeasureFixedFiveArgumentContextCallMetamethodAllocations(
                context,
                callableValue,
                first,
                second,
                third,
                fourth,
                fifth,
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            meta.Set("__call", sixArgumentCallback);
            long sixArgumentAllocated = MeasureFixedSixArgumentContextCallMetamethodAllocations(
                context,
                callableValue,
                first,
                second,
                third,
                fourth,
                fifth,
                sixth,
                iterations
            );
            long fiveExtraBytesPerCall =
                (fiveArgumentAllocated - fourArgumentAllocated) / iterations;
            long sixExtraBytesPerCall = (sixArgumentAllocated - fiveArgumentAllocated) / iterations;

            await Assert
                .That(fiveExtraBytesPerCall)
                .IsLessThan(16L)
                .Because(
                    $"Four-user-argument callable-table calls allocated {fourArgumentAllocated} bytes; five-user-argument calls allocated {fiveArgumentAllocated} bytes."
                );
            await Assert
                .That(sixExtraBytesPerCall)
                .IsLessThan(16L)
                .Because(
                    $"Five-user-argument callable-table calls allocated {fiveArgumentAllocated} bytes; six-user-argument calls allocated {sixArgumentAllocated} bytes."
                );
        }

        [global::TUnit.Core.Test]
        public async Task SixArgumentLegacySpanCallAvoidsArgumentArrayAllocation()
        {
            const int iterations = 1_024;
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue[] sixArgs =
            {
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                LuaValue.NewNumber(4),
                LuaValue.NewNumber(5),
                LuaValue.NewNumber(6),
            };
            LuaValue fiveArgumentLegacyCallback = LuaValue.NewCallback(
                (_, args) =>
                {
                    if (args.Count != 5)
                    {
                        throw new InvalidOperationException(
                            "Context fixed legacy allocation probe received unexpected arity."
                        );
                    }

                    return LuaValue.Nil;
                }
            );
            LuaValue sixArgumentLegacyCallback = LuaValue.NewCallback(
                (_, args) =>
                {
                    if (args.Count != 6)
                    {
                        throw new InvalidOperationException(
                            "Context legacy span allocation probe received unexpected arity."
                        );
                    }

                    return LuaValue.Nil;
                }
            );

            MeasureFixedFiveArgumentContextCallAllocations(
                context,
                fiveArgumentLegacyCallback,
                sixArgs[0],
                sixArgs[1],
                sixArgs[2],
                sixArgs[3],
                sixArgs[4],
                iterations: 8
            );
            MeasureSixArgumentSpanContextCallAllocations(
                context,
                sixArgumentLegacyCallback,
                sixArgs,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long fixedAllocated = MeasureFixedFiveArgumentContextCallAllocations(
                context,
                fiveArgumentLegacyCallback,
                sixArgs[0],
                sixArgs[1],
                sixArgs[2],
                sixArgs[3],
                sixArgs[4],
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long spanAllocated = MeasureSixArgumentSpanContextCallAllocations(
                context,
                sixArgumentLegacyCallback,
                sixArgs,
                iterations
            );
            long extraBytesPerCall = (spanAllocated - fixedAllocated) / iterations;

            await Assert.That(extraBytesPerCall).IsLessThan(16L);
        }

        [global::TUnit.Core.Test]
        public async Task FixedFiveArgumentCallMetamethodPreservesSpecialArgumentAdjustment()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table callable = new(script);
            Table meta = new(script);
            LuaValue callableValue = LuaValue.NewTable(callable);
            LuaValue[] values =
            {
                LuaValue.NewNumber(1),
                LuaValue.Nil,
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                LuaValue.NewNumber(3),
                LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.Nil),
            };

            callable.MetaTable = meta;
            meta.Set(
                "__call",
                LuaValue.NewCallback((_, args) => SummarizeArgumentsSkippingFirst(args))
            );
            LuaValue legacyResult = context.Call(
                callableValue,
                values[0],
                values[1],
                values[2],
                values[3],
                values[4]
            );

            meta.Set(
                "__call",
                LuaValue.NewCallbackView((_, args) => SummarizeArgumentsSkippingFirst(args))
            );
            LuaValue viewResult = context.Call(
                callableValue,
                values[0],
                values[1],
                values[2],
                values[3],
                values[4]
            );

            await AssertArgumentSummary(legacyResult, count: 6d, nilCount: 2d, sum: 10d)
                .ConfigureAwait(false);
            await AssertArgumentSummary(viewResult, count: 6d, nilCount: 2d, sum: 10d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedCallOverloadsAvoidChainedCallMetamethodArgumentArrayAllocation()
        {
            const int iterations = 1_024;
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);
            LuaValue targetValue = LuaValue.NewTable(target);
            LuaValue proxyValue = LuaValue.NewTable(proxy);
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewNumber(2);
            LuaValue third = LuaValue.NewNumber(3);
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    if (
                        args.Count != 5
                        || !ReferenceEquals(args[0].Table, proxy)
                        || !ReferenceEquals(args[1].Table, target)
                        || args[2].Number != 1d
                        || args[3].Number != 2d
                        || args[4].Number != 3d
                    )
                    {
                        throw new InvalidOperationException(
                            "Context chained metamethod allocation probe received unexpected arguments."
                        );
                    }

                    return LuaValue.Nil;
                }
            );
            targetMeta.Set("__call", proxyValue);
            proxyMeta.Set("__call", callback);
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            MeasureFixedFiveArgumentContextCallAllocations(
                context,
                callback,
                proxyValue,
                targetValue,
                first,
                second,
                third,
                iterations: 8
            );
            MeasureFixedThreeArgumentContextChainedCallMetamethodAllocations(
                context,
                targetValue,
                first,
                second,
                third,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long directAllocated = MeasureFixedFiveArgumentContextCallAllocations(
                context,
                callback,
                proxyValue,
                targetValue,
                first,
                second,
                third,
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long metamethodAllocated =
                MeasureFixedThreeArgumentContextChainedCallMetamethodAllocations(
                    context,
                    targetValue,
                    first,
                    second,
                    third,
                    iterations
                );
            long extraBytesPerCall = (metamethodAllocated - directAllocated) / iterations;

            await Assert.That(extraBytesPerCall).IsLessThan(16L);
        }

        [global::TUnit.Core.Test]
        public async Task FixedFiveArgumentCallOverloadAvoidsChainedCallMetamethodArgumentArrayAllocation()
        {
            const int iterations = 1_024;
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);
            LuaValue targetValue = LuaValue.NewTable(target);
            LuaValue proxyValue = LuaValue.NewTable(proxy);
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewNumber(2);
            LuaValue third = LuaValue.NewNumber(3);
            LuaValue fourth = LuaValue.NewNumber(4);
            LuaValue fifth = LuaValue.NewNumber(5);
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    if (
                        (args.Count != 6 && args.Count != 7)
                        || !ReferenceEquals(args[0].Table, proxy)
                        || !ReferenceEquals(args[1].Table, target)
                    )
                    {
                        throw new InvalidOperationException(
                            "Context five-argument chained metamethod allocation probe received unexpected self arguments."
                        );
                    }

                    for (int i = 2; i < args.Count; i++)
                    {
                        if (args[i].Number != i - 1d)
                        {
                            throw new InvalidOperationException(
                                "Context five-argument chained metamethod allocation probe received unexpected user arguments."
                            );
                        }
                    }

                    return LuaValue.Nil;
                }
            );
            targetMeta.Set("__call", proxyValue);
            proxyMeta.Set("__call", callback);
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            MeasureFixedFourArgumentContextChainedCallMetamethodAllocations(
                context,
                targetValue,
                first,
                second,
                third,
                fourth,
                iterations: 8
            );
            MeasureFixedFiveArgumentContextChainedCallMetamethodAllocations(
                context,
                targetValue,
                first,
                second,
                third,
                fourth,
                fifth,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long fourArgumentAllocated =
                MeasureFixedFourArgumentContextChainedCallMetamethodAllocations(
                    context,
                    targetValue,
                    first,
                    second,
                    third,
                    fourth,
                    iterations
                );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long fiveArgumentAllocated =
                MeasureFixedFiveArgumentContextChainedCallMetamethodAllocations(
                    context,
                    targetValue,
                    first,
                    second,
                    third,
                    fourth,
                    fifth,
                    iterations
                );
            long extraBytesPerCall = (fiveArgumentAllocated - fourArgumentAllocated) / iterations;

            await Assert
                .That(extraBytesPerCall)
                .IsLessThan(16L)
                .Because(
                    $"Four-user-argument context chained calls allocated {fourArgumentAllocated} bytes; five-user-argument context chained calls allocated {fiveArgumentAllocated} bytes."
                );
        }

        [global::TUnit.Core.Test]
        public async Task FixedSixArgumentCallOverloadAvoidsChainedCallMetamethodFallbackArgumentArrayAllocation()
        {
            const int iterations = 1_024;
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);
            LuaValue targetValue = LuaValue.NewTable(target);
            LuaValue proxyValue = LuaValue.NewTable(proxy);
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewNumber(2);
            LuaValue third = LuaValue.NewNumber(3);
            LuaValue fourth = LuaValue.NewNumber(4);
            LuaValue fifth = LuaValue.NewNumber(5);
            LuaValue sixth = LuaValue.NewNumber(6);
            int expectedCount = 0;
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    if (expectedCount == 8)
                    {
                        if (
                            !args.TryGetSpan(out ReadOnlySpan<LuaValue> span)
                            || span.Length != expectedCount
                            || !ReferenceEquals(span[0].Table, proxy)
                            || !ReferenceEquals(span[1].Table, target)
                        )
                        {
                            throw new InvalidOperationException(
                                "Context six-argument chained metamethod allocation probe received unexpected span/self arguments."
                            );
                        }

                        for (int i = 2; i < span.Length; i++)
                        {
                            if (span[i].Number != i - 1d)
                            {
                                throw new InvalidOperationException(
                                    "Context six-argument chained metamethod allocation probe received unexpected user arguments."
                                );
                            }
                        }
                    }
                    else if (
                        args.Count != expectedCount
                        || !ReferenceEquals(args[0].Table, proxy)
                        || !ReferenceEquals(args[1].Table, target)
                    )
                    {
                        throw new InvalidOperationException(
                            "Context six-argument chained metamethod allocation probe received unexpected fixed/self arguments."
                        );
                    }
                    else
                    {
                        for (int i = 2; i < args.Count; i++)
                        {
                            if (args[i].Number != i - 1d)
                            {
                                throw new InvalidOperationException(
                                    "Context six-argument chained metamethod allocation probe received unexpected user arguments."
                                );
                            }
                        }
                    }

                    return LuaValue.Nil;
                }
            );
            targetMeta.Set("__call", proxyValue);
            proxyMeta.Set("__call", callback);
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            expectedCount = 7;
            MeasureFixedFiveArgumentContextChainedCallMetamethodAllocations(
                context,
                targetValue,
                first,
                second,
                third,
                fourth,
                fifth,
                iterations: 8
            );
            expectedCount = 8;
            MeasureFixedSixArgumentContextChainedCallMetamethodAllocations(
                context,
                targetValue,
                first,
                second,
                third,
                fourth,
                fifth,
                sixth,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            expectedCount = 7;
            long fiveArgumentAllocated =
                MeasureFixedFiveArgumentContextChainedCallMetamethodAllocations(
                    context,
                    targetValue,
                    first,
                    second,
                    third,
                    fourth,
                    fifth,
                    iterations
                );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            expectedCount = 8;
            long sixArgumentAllocated =
                MeasureFixedSixArgumentContextChainedCallMetamethodAllocations(
                    context,
                    targetValue,
                    first,
                    second,
                    third,
                    fourth,
                    fifth,
                    sixth,
                    iterations
                );
            long extraBytesPerCall = (sixArgumentAllocated - fiveArgumentAllocated) / iterations;

            await Assert
                .That(extraBytesPerCall)
                .IsLessThan(16L)
                .Because(
                    $"Five-user-argument context chained calls allocated {fiveArgumentAllocated} bytes; six-user-argument context chained calls allocated {sixArgumentAllocated} bytes."
                );
        }

        [global::TUnit.Core.Test]
        public async Task SpanAndArrayCallOverloadsAvoidCallMetamethodArgumentArrayAllocation()
        {
            const int iterations = 1_024;
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table callable = new(script);
            Table meta = new(script);
            LuaValue callableValue = LuaValue.NewTable(callable);
            LuaValue[] args =
            {
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                LuaValue.NewNumber(4),
            };
            LuaValue callback = LuaValue.NewCallbackView(
                (_, callbackArgs) =>
                {
                    if (
                        callbackArgs.Count != 5
                        || !ReferenceEquals(callbackArgs[0].Table, callable)
                        || callbackArgs[1].Number != 1d
                        || callbackArgs[2].Number != 2d
                        || callbackArgs[3].Number != 3d
                        || callbackArgs[4].Number != 4d
                    )
                    {
                        throw new InvalidOperationException(
                            "Context span/array metamethod allocation probe received unexpected arguments."
                        );
                    }

                    return LuaValue.Nil;
                }
            );
            meta.Set("__call", callback);
            callable.MetaTable = meta;

            MeasureFixedFiveArgumentContextCallAllocations(
                context,
                callback,
                callableValue,
                args[0],
                args[1],
                args[2],
                args[3],
                iterations: 8
            );
            MeasureSpanContextCallMetamethodAllocations(
                context,
                callableValue,
                args,
                iterations: 8
            );
            MeasureArrayContextCallMetamethodAllocations(
                context,
                callableValue,
                args,
                iterations: 8
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long directAllocated = MeasureFixedFiveArgumentContextCallAllocations(
                context,
                callback,
                callableValue,
                args[0],
                args[1],
                args[2],
                args[3],
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long spanAllocated = MeasureSpanContextCallMetamethodAllocations(
                context,
                callableValue,
                args,
                iterations
            );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long arrayAllocated = MeasureArrayContextCallMetamethodAllocations(
                context,
                callableValue,
                args,
                iterations
            );
            long spanExtraBytesPerCall = (spanAllocated - directAllocated) / iterations;
            long arrayExtraBytesPerCall = (arrayAllocated - directAllocated) / iterations;

            await Assert.That(spanExtraBytesPerCall).IsLessThan(16L);
            await Assert.That(arrayExtraBytesPerCall).IsLessThan(16L);
        }

        [global::TUnit.Core.Test]
        public async Task SpanAndArrayCallOverloadsPreserveCallMetamethodSpecialArgumentAdjustment()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table callable = new(script);
            Table meta = new(script);
            LuaValue callableValue = LuaValue.NewTable(callable);
            LuaValue inspect = LuaValue.NewCallback((_, args) => SummarizeArguments(args));
            meta.Set("__call", inspect);
            callable.MetaTable = meta;
            LuaValue[] spanArgs =
            {
                LuaValue.Nil,
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                LuaValue.NewNumber(3),
                LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.Nil),
            };
            LuaValue[] arrayArgs =
            {
                LuaValue.NewNumber(1),
                LuaValue.Nil,
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                LuaValue.NewNumber(3),
                LuaValue.Void,
            };

            LuaValue spanResult = context.Call(callableValue, spanArgs.AsSpan());
            LuaValue arrayResult = context.Call(callableValue, arrayArgs);

            await AssertArgumentSummary(spanResult, count: 6d, nilCount: 2d, sum: 9d)
                .ConfigureAwait(false);
            await AssertArgumentSummary(arrayResult, count: 5d, nilCount: 1d, sum: 6d)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FixedCallOverloadsPreserveLegacyCallbackExpansionSemantics()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue inspect = LuaValue.NewCallback(
                (_, args) =>
                    LuaValue.NewTuple(LuaValue.NewNumber(args.Count), args[0], args[1], args[2])
            );
            LuaValue countVoid = LuaValue.NewCallback((_, args) => LuaValue.NewNumber(args.Count));

            LuaValue expanded = context.Call(
                inspect,
                LuaValue.Nil,
                LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.Nil)
            );
            LuaValue voidTrimmed = context.Call(countVoid, LuaValue.NewNumber(1), LuaValue.Void);

            await Assert.That(expanded.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(expanded.Tuple.Length).IsEqualTo(4);
            await Assert.That(expanded.Tuple[0].Number).IsEqualTo(3d);
            await Assert.That(expanded.Tuple[1].Type).IsEqualTo(DataType.Nil);
            await Assert.That(expanded.Tuple[2].Number).IsEqualTo(2d);
            await Assert.That(expanded.Tuple[3].Type).IsEqualTo(DataType.Nil);
            await Assert.That(voidTrimmed.Number).IsEqualTo(1d);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(2)]
        [global::TUnit.Core.Arguments(3)]
        [global::TUnit.Core.Arguments(4)]
        [global::TUnit.Core.Arguments(5)]
        [global::TUnit.Core.Arguments(6)]
        [global::TUnit.Core.Arguments(7)]
        public async Task FixedCallOverloadsPreserveLegacyCallbackArity(int arity)
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue inspect = LuaValue.NewCallback(
                (_, args) =>
                {
                    double sum = 0d;
                    for (int i = 0; i < args.Count; i++)
                    {
                        sum += args[i].Number;
                    }

                    return LuaValue.NewTuple(
                        LuaValue.NewNumber(args.Count),
                        LuaValue.NewNumber(sum)
                    );
                }
            );

            LuaValue result = CallWithFixedArguments(
                context,
                inspect,
                CreateSequentialArguments(arity)
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Number).IsEqualTo((double)arity);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(arity * (arity + 1) / 2d);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(2)]
        [global::TUnit.Core.Arguments(3)]
        [global::TUnit.Core.Arguments(4)]
        [global::TUnit.Core.Arguments(5)]
        [global::TUnit.Core.Arguments(6)]
        [global::TUnit.Core.Arguments(7)]
        public async Task FixedCallOverloadsPreserveLegacyCallbackSpecialArguments(int arity)
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue inspect = LuaValue.NewCallback(
                (_, args) =>
                {
                    double nilCount = 0d;
                    double sum = 0d;

                    for (int i = 0; i < args.Count; i++)
                    {
                        LuaValue arg = args[i];
                        if (arg.Type == DataType.Nil)
                        {
                            nilCount++;
                        }
                        else
                        {
                            sum += arg.Number;
                        }
                    }

                    return LuaValue.NewTuple(
                        LuaValue.NewNumber(args.Count),
                        LuaValue.NewNumber(nilCount),
                        LuaValue.NewNumber(sum)
                    );
                }
            );

            LuaValue[] values = arity switch
            {
                1 => new[] { LuaValue.Nil },
                2 => new[] { LuaValue.NewNumber(1), LuaValue.Void },
                3 => new[]
                {
                    LuaValue.NewNumber(1),
                    LuaValue.NewNumber(2),
                    LuaValue.NewTuple(LuaValue.NewNumber(3), LuaValue.Nil),
                },
                4 => new LuaValue[]
                {
                    LuaValue.Nil,
                    LuaValue.NewNumber(2),
                    LuaValue.NewNumber(3),
                    LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.Nil),
                },
                5 => new[]
                {
                    LuaValue.NewNumber(1),
                    LuaValue.Nil,
                    LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                    LuaValue.NewNumber(3),
                    LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.Nil),
                },
                6 => new[]
                {
                    LuaValue.NewNumber(1),
                    LuaValue.Nil,
                    LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                    LuaValue.NewNumber(3),
                    LuaValue.NewNumber(5),
                    LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.Nil),
                },
                7 => new[]
                {
                    LuaValue.NewNumber(1),
                    LuaValue.Nil,
                    LuaValue.NewTuple(LuaValue.NewNumber(2), LuaValue.NewNumber(20)),
                    LuaValue.NewNumber(3),
                    LuaValue.NewTuple(LuaValue.NewNumber(4), LuaValue.NewNumber(40)),
                    LuaValue.NewNumber(5),
                    LuaValue.NewTuple(LuaValue.NewNumber(6), LuaValue.Nil),
                },
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };
            LuaValue result = CallWithFixedArguments(context, inspect, values);

            double expectedCount = arity switch
            {
                1 => 1d,
                2 => 1d,
                3 => 4d,
                4 => 5d,
                5 => 6d,
                6 => 7d,
                7 => 8d,
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };
            double expectedNilCount = arity switch
            {
                1 => 1d,
                2 => 0d,
                3 => 1d,
                4 => 2d,
                5 => 2d,
                6 => 2d,
                7 => 2d,
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };
            double expectedSum = arity switch
            {
                1 => 0d,
                2 => 1d,
                3 => 6d,
                4 => 9d,
                5 => 10d,
                6 => 15d,
                7 => 21d,
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(expectedCount);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(expectedNilCount);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(expectedSum);
        }

        [global::TUnit.Core.Test]
        public async Task FixedCallOverloadsRejectTailCallWithContinuation()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue func = LuaValue.NewCallbackView(
                (_, _) =>
                    LuaValue.NewTailCallReq(
                        new TailCallData
                        {
                            Function = LuaValue.NewCallback((_, _) => LuaValue.NewNumber(1)),
                            Continuation = new CallbackFunction((_, _) => LuaValue.Nil),
                        }
                    )
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(func, LuaValue.NewNumber(1))
            );
            await Assert.That(exception.Message).Contains("cannot be called directly");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(6)]
        [global::TUnit.Core.Arguments(7)]
        public async Task CallUsesCallMetamethod(int arity)
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table target = new(script);
            Table meta = new(script);
            meta.Set(
                "__call",
                LuaValue.NewCallback(
                    (_, args) =>
                    {
                        double sum = 0d;
                        for (int i = 1; i < args.Count; i++)
                        {
                            sum += args[i].Number;
                        }

                        return LuaValue.NewTuple(
                            LuaValue.NewBoolean(args.Count == arity + 1),
                            LuaValue.NewBoolean(ReferenceEquals(args[0].Table, target)),
                            LuaValue.NewNumber(sum)
                        );
                    }
                )
            );
            target.MetaTable = meta;

            LuaValue result = CallWithFixedArguments(
                context,
                LuaValue.NewTable(target),
                CreateSequentialArguments(arity)
            );
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Boolean).IsTrue();
            await Assert.That(result.Tuple[2].Number).IsEqualTo(arity * (arity + 1) / 2d);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task CallRejectsChainedCallMetamethodsBeforeLua54(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);
            targetMeta.Set("__call", LuaValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                LuaValue.NewCallback((_, _) => LuaValue.NewString("unexpected"))
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(LuaValue.NewTable(target), LuaValue.NewNumber(9))
            );

            await Assert.That(exception.Message).Contains("attempt to call");
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task CallFollowsChainedCallMetamethodsWithSelfArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);
            targetMeta.Set("__call", LuaValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                LuaValue.NewCallback(
                    (_, args) =>
                        LuaValue.NewTuple(
                            LuaValue.NewBoolean(args.Count == 3),
                            LuaValue.NewBoolean(ReferenceEquals(args[0].Table, proxy)),
                            LuaValue.NewBoolean(ReferenceEquals(args[1].Table, target)),
                            args[2]
                        )
                )
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            LuaValue result = context.Call(LuaValue.NewTable(target), LuaValue.NewNumber(9));

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Boolean).IsTrue();
            await Assert.That(result.Tuple[2].Boolean).IsTrue();
            await Assert.That(result.Tuple[3].Number).IsEqualTo(9d);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task CallFollowsChainedCallMetamethodsWhenFixedArgumentBufferIsFull(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);
            targetMeta.Set("__call", LuaValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                LuaValue.NewCallback(
                    (_, args) =>
                    {
                        double sum = 0d;
                        for (int i = 2; i < args.Count; i++)
                        {
                            sum += args[i].Number;
                        }

                        return LuaValue.NewTuple(
                            LuaValue.NewNumber(args.Count),
                            LuaValue.NewBoolean(ReferenceEquals(args[0].Table, proxy)),
                            LuaValue.NewBoolean(ReferenceEquals(args[1].Table, target)),
                            LuaValue.NewNumber(sum)
                        );
                    }
                )
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            LuaValue result = context.Call(
                LuaValue.NewTable(target),
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3),
                LuaValue.NewNumber(4)
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(6d);
            await Assert.That(result.Tuple[1].Boolean).IsTrue();
            await Assert.That(result.Tuple[2].Boolean).IsTrue();
            await Assert.That(result.Tuple[3].Number).IsEqualTo(10d);
        }

        [global::TUnit.Core.Test]
        public async Task DefaultCallFollowsChainedCallMetamethodsWithSelfArguments()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table target = new(script);
            Table proxy = new(script);
            Table targetMeta = new(script);
            Table proxyMeta = new(script);
            targetMeta.Set("__call", LuaValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                LuaValue.NewCallback(
                    (_, args) =>
                        LuaValue.NewTuple(
                            LuaValue.NewBoolean(args.Count == 3),
                            LuaValue.NewBoolean(ReferenceEquals(args[0].Table, proxy)),
                            LuaValue.NewBoolean(ReferenceEquals(args[1].Table, target)),
                            args[2]
                        )
                )
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            LuaValue result = context.Call(LuaValue.NewTable(target), LuaValue.NewNumber(9));

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Boolean).IsTrue();
            await Assert.That(result.Tuple[2].Boolean).IsTrue();
            await Assert.That(result.Tuple[3].Number).IsEqualTo(9d);
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateSymbolReturnsNilWhenMissing()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            LuaValue nil = context.EvaluateSymbol(null);
            await Assert.That(nil).IsEqualTo(LuaValue.Nil);
        }

        [global::TUnit.Core.Test]
        public async Task GetMetatableThrowsWhenValueIsNull()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            Table metatable = context.GetMetatable(LuaValue.Nil);

            await Assert.That(metatable).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task GetMetamethodThrowsWhenArgumentsNull()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue value = LuaValue.NewNumber(1);

            LuaValue missing = context.GetMetamethod(LuaValue.Nil, "__call");
            bool found = context.TryGetMetamethod(LuaValue.Nil, "__call", out LuaValue resolved);
            ArgumentNullException methodException = ExpectException<ArgumentNullException>(() =>
                context.GetMetamethod(value, null)
            );
            ArgumentNullException tryMethodException = ExpectException<ArgumentNullException>(() =>
                context.TryGetMetamethod(value, null, out LuaValue _)
            );

            await Assert.That(missing.IsNil).IsTrue();
            await Assert.That(found).IsFalse();
            await Assert.That(resolved.IsNil).IsTrue();
            await Assert.That(methodException.ParamName).IsEqualTo("metamethod");
            await Assert.That(tryMethodException.ParamName).IsEqualTo("metamethod");
        }

        [global::TUnit.Core.Test]
        public async Task GetBinaryMetamethodValidatesArguments()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            LuaValue operand = LuaValue.NewNumber(1);

            LuaValue nilOperandMetamethod = context.GetBinaryMetamethod(
                LuaValue.Nil,
                operand,
                "__add"
            );
            bool foundNilOperand = context.TryGetBinaryMetamethod(
                operand,
                LuaValue.Nil,
                "__add",
                out LuaValue nilOperandResolved
            );
            ArgumentNullException eventException = ExpectException<ArgumentNullException>(() =>
                context.GetBinaryMetamethod(operand, operand, null)
            );
            ArgumentNullException tryEventException = ExpectException<ArgumentNullException>(() =>
                context.TryGetBinaryMetamethod(operand, operand, null, out LuaValue _)
            );

            Table left = new(script);
            Table meta = new(script);
            LuaValue addMetamethod = LuaValue.NewCallback((_, _) => LuaValue.Nil);
            meta.Set("__add", addMetamethod);
            left.MetaTable = meta;

            bool found = context.TryGetBinaryMetamethod(
                LuaValue.NewTable(left),
                operand,
                "__add",
                out LuaValue resolved
            );
            bool missing = context.TryGetBinaryMetamethod(
                operand,
                operand,
                "__add",
                out LuaValue absent
            );
            LuaValue legacyResolved = context.GetBinaryMetamethod(
                LuaValue.NewTable(left),
                operand,
                "__add"
            );
            LuaValue legacyAbsent = context.GetBinaryMetamethod(operand, operand, "__add");
            meta.Set("__sub", LuaValue.Nil);
            bool foundExplicitNil = context.TryGetBinaryMetamethod(
                LuaValue.NewTable(left),
                operand,
                "__sub",
                out LuaValue explicitNil
            );
            PresenceAwareMetamethodDescriptor descriptor = new()
            {
                HandlesMetaIndex = true,
                MetaIndexValue = LuaValue.Void,
            };
            LuaValue descriptorOperand = UserData.Create(new object(), descriptor);
            bool foundDescriptorVoid = context.TryGetBinaryMetamethod(
                descriptorOperand,
                operand,
                "__add",
                out LuaValue descriptorVoid
            );
            descriptor.HandlesMetaIndex = false;
            bool missingDescriptorMeta = context.TryGetBinaryMetamethod(
                descriptorOperand,
                operand,
                "__add",
                out LuaValue missingDescriptorValue
            );

            await Assert.That(nilOperandMetamethod.IsNil).IsTrue();
            await Assert.That(foundNilOperand).IsFalse();
            await Assert.That(nilOperandResolved.IsNil).IsTrue();
            await Assert.That(eventException.ParamName).IsEqualTo("eventName");
            await Assert.That(tryEventException.ParamName).IsEqualTo("eventName");
            await Assert.That(found).IsTrue();
            await Assert.That(resolved).IsEqualTo(addMetamethod);
            await Assert.That(missing).IsFalse();
            await Assert.That(absent.IsNil).IsTrue();
            await Assert.That(legacyResolved).IsEqualTo(addMetamethod);
            await Assert.That(legacyAbsent.IsNil).IsTrue();
            await Assert.That(foundExplicitNil).IsFalse();
            await Assert.That(explicitNil.IsNil).IsTrue();
            await Assert.That(foundDescriptorVoid).IsTrue();
            await Assert.That(descriptorVoid.IsVoid()).IsTrue();
            await Assert.That(missingDescriptorMeta).IsFalse();
            await Assert.That(missingDescriptorValue.IsNil).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task EmulateClassicCallValidatesArguments()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(Array.Empty<LuaValue>(), false);

            ArgumentNullException argsException = ExpectException<ArgumentNullException>(() =>
                context.EmulateClassicCall(null, "fn", _ => 0)
            );
            ArgumentNullException callbackException = ExpectException<ArgumentNullException>(() =>
                context.EmulateClassicCall(args, "fn", null)
            );

            await Assert.That(argsException.ParamName).IsEqualTo("args");
            await Assert.That(callbackException.ParamName).IsEqualTo("callback");
        }

        [global::TUnit.Core.Test]
        public async Task CallValidatesFunctionArgument()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(LuaValue.Nil)
            );

            await Assert.That(exception.Message).Contains("call");
            await Assert.That(exception.Message).Contains("nil");
        }

        [global::TUnit.Core.Test]
        public async Task GetMetamethodTailCallReturnsNullWhenMissing()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table targetTable = new(script);
            LuaValue target = LuaValue.NewTable(targetTable);

            bool found = context.TryGetMetamethod(target, "__call", out LuaValue metamethod);
            LuaValue legacyMetamethod = context.GetMetamethod(target, "__call");
            LuaValue tail = context.GetMetamethodTailCall(target, "__call");
            await Assert.That(found).IsFalse();
            await Assert.That(metamethod.IsNil).IsTrue();
            await Assert.That(legacyMetamethod.IsNil).IsTrue();
            await Assert.That(tail.IsNil).IsTrue();

            Table metatable = new(script);
            metatable.Set("__call", LuaValue.Nil);
            targetTable.MetaTable = metatable;

            bool foundExplicitNil = context.TryGetMetamethod(
                target,
                "__call",
                out LuaValue explicitNil
            );
            await Assert.That(foundExplicitNil).IsFalse();
            await Assert.That(explicitNil.IsNil).IsTrue();

            PresenceAwareMetamethodDescriptor descriptor = new()
            {
                HandlesMetaIndex = true,
                MetaIndexValue = LuaValue.Nil,
            };
            LuaValue descriptorTarget = UserData.Create(new object(), descriptor);
            bool foundDescriptorNil = context.TryGetMetamethod(
                descriptorTarget,
                "__call",
                out LuaValue descriptorNil
            );
            LuaValue legacyDescriptorNil = context.GetMetamethod(descriptorTarget, "__call");

            await Assert.That(foundDescriptorNil).IsTrue();
            await Assert.That(descriptorNil.IsNil).IsTrue();
            await Assert.That(legacyDescriptorNil.IsNil).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task PerformMessageDecorationDefaultsToOriginal()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            ScriptRuntimeException exception = new("boom");

            ScriptExecutionContext.PerformMessageDecorationBeforeUnwind(exception);
            await Assert.That(exception.DecoratedMessage).IsEqualTo("boom");
        }

        [global::TUnit.Core.Test]
        public async Task IsYieldableReturnsFalseForDynamicContexts()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            await Assert.That(context.IsYieldable()).IsFalse();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task IsYieldableReturnsFalseForMainProcessor(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue callback = LuaValue.NewCallback(
                (context, _) => LuaValue.NewBoolean(context.IsYieldable())
            );
            script.Globals["yieldState"] = callback;

            LuaValue result = script.DoString("return yieldState()");
            await Assert.That(result.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task IsYieldableReturnsTrueInsideCoroutine(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue callback = LuaValue.NewCallback(
                (context, _) => LuaValue.NewBoolean(context.IsYieldable())
            );
            script.Globals["yieldState"] = callback;
            script.DoString("function coroutineProbe() return yieldState() end");

            LuaValue coroutineHandle = script.CreateCoroutineValue(
                script.Globals.Get("coroutineProbe")
            );
            LuaValue resumeResult = coroutineHandle.Coroutine.Resume();

            await Assert.That(resumeResult.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task CallThrowsLoopInCallWhenCallMetamethodChainExceedsLimit(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            // Create a chain of tables where each __call metamethod returns another table
            // with a __call metamethod, exceeding the 10-iteration limit
            Table root = new(script);
            Table current = root;
            for (int i = 0; i < 15; i++)
            {
                Table next = new(script);
                Table meta = new(script);
                meta.Set("__call", LuaValue.NewTable(next));
                current.MetaTable = meta;
                current = next;
            }

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(LuaValue.NewTable(root))
            );

            await Assert.That(exception.Message).Contains("loop");
        }

        [global::TUnit.Core.Test]
        public async Task CallThrowsAttemptToCallNonFuncWhenCallMetamethodIsNil()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            // A table without __call metamethod
            Table target = new(script);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(LuaValue.NewTable(target))
            );

            await Assert.That(exception.Message).Contains("attempt to call");
        }

        [global::TUnit.Core.Test]
        public async Task CallThrowsAttemptToCallNonFuncWhenCallMetamethodReturnsNil()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            // A table with __call metamethod that returns nil
            Table target = new(script);
            Table meta = new(script);
            meta.Set("__call", LuaValue.Nil);
            target.MetaTable = meta;

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(LuaValue.NewTable(target))
            );

            await Assert.That(exception.Message).Contains("attempt to call");
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }

        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            return new Script(version, CoreModulePresets.Complete);
        }

        private static LuaValue SummarizeArguments(CallbackArguments args)
        {
            double nilCount = 0d;
            double sum = 0d;

            for (int i = 0; i < args.Count; i++)
            {
                LuaValue arg = args[i];
                if (arg.Type == DataType.Nil)
                {
                    nilCount++;
                }
                else
                {
                    sum += arg.Number;
                }
            }

            return LuaValue.NewTuple(
                LuaValue.NewNumber(args.Count),
                LuaValue.NewNumber(nilCount),
                LuaValue.NewNumber(sum)
            );
        }

        private static LuaValue SummarizeArgumentsSkippingFirst(CallbackArguments args)
        {
            double nilCount = 0d;
            double sum = 0d;

            for (int i = 1; i < args.Count; i++)
            {
                LuaValue arg = args[i];
                if (arg.Type == DataType.Nil)
                {
                    nilCount++;
                }
                else
                {
                    sum += arg.Number;
                }
            }

            return LuaValue.NewTuple(
                LuaValue.NewNumber(Math.Max(args.Count - 1, 0)),
                LuaValue.NewNumber(nilCount),
                LuaValue.NewNumber(sum)
            );
        }

        private static LuaValue SummarizeArgumentsSkippingFirst(CallbackArgumentsView args)
        {
            double nilCount = 0d;
            double sum = 0d;

            for (int i = 1; i < args.Count; i++)
            {
                LuaValue arg = args[i];
                if (arg.Type == DataType.Nil)
                {
                    nilCount++;
                }
                else
                {
                    sum += arg.Number;
                }
            }

            return LuaValue.NewTuple(
                LuaValue.NewNumber(Math.Max(args.Count - 1, 0)),
                LuaValue.NewNumber(nilCount),
                LuaValue.NewNumber(sum)
            );
        }

        private static async Task AssertArgumentSummary(
            LuaValue value,
            double count,
            double nilCount,
            double sum
        )
        {
            await Assert.That(value.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(value.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(value.Tuple[0].Number).IsEqualTo(count).ConfigureAwait(false);
            await Assert.That(value.Tuple[1].Number).IsEqualTo(nilCount).ConfigureAwait(false);
            await Assert.That(value.Tuple[2].Number).IsEqualTo(sum).ConfigureAwait(false);
        }

        private static LuaValue[] CreateSequentialArguments(int arity)
        {
            LuaValue[] args = new LuaValue[arity];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = LuaValue.NewNumber(i + 1d);
            }

            return args;
        }

        private static LuaValue CallWithFixedArguments(
            ScriptExecutionContext context,
            LuaValue callback,
            LuaValue[] args
        )
        {
            return args.Length switch
            {
                0 => context.Call(callback),
                1 => context.Call(callback, args[0]),
                2 => context.Call(callback, args[0], args[1]),
                3 => context.Call(callback, args[0], args[1], args[2]),
                4 => context.Call(callback, args[0], args[1], args[2], args[3]),
                5 => context.Call(callback, args[0], args[1], args[2], args[3], args[4]),
                6 => context.Call(callback, args[0], args[1], args[2], args[3], args[4], args[5]),
                7 => context.Call(
                    callback,
                    args[0],
                    args[1],
                    args[2],
                    args[3],
                    args[4],
                    args[5],
                    args[6]
                ),
                _ => context.Call(callback, args.AsSpan()),
            };
        }

        private static long MeasureNoArgumentContextCallAllocations(
            ScriptExecutionContext context,
            LuaValue callback,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(callback);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "No-argument context call allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedFiveArgumentContextCallAllocations(
            ScriptExecutionContext context,
            LuaValue callback,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            LuaValue fifth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(callback, first, second, third, fourth, fifth);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Fixed-argument context call allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedSixArgumentContextCallAllocations(
            ScriptExecutionContext context,
            LuaValue callback,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            LuaValue fifth,
            LuaValue sixth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(
                    callback,
                    first,
                    second,
                    third,
                    fourth,
                    fifth,
                    sixth
                );
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Fixed six-argument context call allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedSevenArgumentContextCallAllocations(
            ScriptExecutionContext context,
            LuaValue callback,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            LuaValue fifth,
            LuaValue sixth,
            LuaValue seventh,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(
                    callback,
                    first,
                    second,
                    third,
                    fourth,
                    fifth,
                    sixth,
                    seventh
                );
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Fixed seven-argument context call allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedFourArgumentContextCallMetamethodAllocations(
            ScriptExecutionContext context,
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(callable, first, second, third, fourth);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Context metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedFiveArgumentContextCallMetamethodAllocations(
            ScriptExecutionContext context,
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            LuaValue fifth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(callable, first, second, third, fourth, fifth);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Context five-argument metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedSixArgumentContextCallMetamethodAllocations(
            ScriptExecutionContext context,
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            LuaValue fifth,
            LuaValue sixth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(
                    callable,
                    first,
                    second,
                    third,
                    fourth,
                    fifth,
                    sixth
                );
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Context six-argument metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureSixArgumentSpanContextCallAllocations(
            ScriptExecutionContext context,
            LuaValue callback,
            LuaValue[] args,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(callback, args.AsSpan());
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Context six-argument span allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private sealed class PresenceAwareMetamethodDescriptor : IUserDataDescriptorTryAccess
        {
            internal bool HandlesMetaIndex { get; set; }

            internal LuaValue MetaIndexValue { get; set; } = LuaValue.Nil;

            public string Name => nameof(PresenceAwareMetamethodDescriptor);

            public Type Type => typeof(object);

            public LuaValue Index(Script script, object obj, LuaValue index, bool isDirectIndexing)
            {
                return TryIndex(script, obj, index, isDirectIndexing, out LuaValue value)
                    ? value
                    : LuaValue.Nil;
            }

            public bool TryIndex(
                Script script,
                object obj,
                LuaValue index,
                bool isDirectIndexing,
                out LuaValue value
            )
            {
                value = LuaValue.Nil;
                return false;
            }

            public bool SetIndex(
                Script script,
                object obj,
                LuaValue index,
                LuaValue value,
                bool isDirectIndexing
            )
            {
                return false;
            }

            public string AsString(object obj)
            {
                return Name;
            }

            public LuaValue MetaIndex(Script script, object obj, string metaname)
            {
                return HandlesMetaIndex ? MetaIndexValue : LuaValue.Nil;
            }

            public bool TryMetaIndex(Script script, object obj, string metaname, out LuaValue value)
            {
                if (HandlesMetaIndex)
                {
                    value = MetaIndexValue;
                    return true;
                }

                value = LuaValue.Nil;
                return false;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return type.IsInstanceOfType(obj);
            }
        }

        private static long MeasureFixedThreeArgumentContextChainedCallMetamethodAllocations(
            ScriptExecutionContext context,
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(callable, first, second, third);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Context chained metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedFourArgumentContextChainedCallMetamethodAllocations(
            ScriptExecutionContext context,
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(callable, first, second, third, fourth);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Context four-argument chained metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedFiveArgumentContextChainedCallMetamethodAllocations(
            ScriptExecutionContext context,
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            LuaValue fifth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(callable, first, second, third, fourth, fifth);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Context five-argument chained metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedSixArgumentContextChainedCallMetamethodAllocations(
            ScriptExecutionContext context,
            LuaValue callable,
            LuaValue first,
            LuaValue second,
            LuaValue third,
            LuaValue fourth,
            LuaValue fifth,
            LuaValue sixth,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(
                    callable,
                    first,
                    second,
                    third,
                    fourth,
                    fifth,
                    sixth
                );
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Context six-argument chained metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureSpanContextCallMetamethodAllocations(
            ScriptExecutionContext context,
            LuaValue callable,
            LuaValue[] args,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(callable, args.AsSpan());
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Context span metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureArrayContextCallMetamethodAllocations(
            ScriptExecutionContext context,
            LuaValue callable,
            LuaValue[] args,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                LuaValue result = context.Call(callable, args);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Context array metamethod allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
    }
}
