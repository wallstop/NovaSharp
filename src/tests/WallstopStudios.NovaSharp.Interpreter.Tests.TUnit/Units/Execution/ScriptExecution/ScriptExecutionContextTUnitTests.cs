namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class ScriptExecutionContextTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task EvaluateSymbolByNameResolvesLocals(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue callback = DynValue.NewCallback(
                (context, _) =>
                {
                    DynValue local = context.EvaluateSymbolByName("localValue");
                    return local ?? DynValue.Nil;
                }
            );
            script.Globals["assertLocal"] = callback;

            DynValue result = script.DoString(
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
            script.Globals["marker"] = DynValue.NewString("available");

            DynValue callback = DynValue.NewCallback(
                (context, _) =>
                {
                    Table env = context.CurrentGlobalEnv;
                    DynValue marker = env.Get("marker");
                    SymbolRef envSymbol = context.FindSymbolByName(WellKnownSymbols.ENV);
                    DynValue envValue = context.EvaluateSymbol(envSymbol);
                    return DynValue.NewTuple(marker, envValue);
                }
            );
            script.Globals["probeEnv"] = callback;

            DynValue tuple = script.DoString(
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

            DynValue callback = DynValue.NewCallback(
                (context, args) =>
                {
                    Table meta = context.GetMetatable(args[0]);
                    return meta?.Get("marker") ?? DynValue.Nil;
                }
            );
            script.Globals["probeMeta"] = callback;

            DynValue marker = script.DoString(
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
            LastTailCall = null;
            DynValue callback = DynValue.NewCallback(
                (context, args) =>
                {
                    LastTailCall = context.GetMetamethodTailCall(
                        args[0],
                        "__call",
                        args[0],
                        args[1]
                    );
                    return DynValue.NewNumber(0);
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

            DynValue tail = LastTailCall;
            await Assert.That(tail).IsNotNull();
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

            DynValue callback = DynValue.NewCallback(
                (context, _) =>
                {
                    ScriptRuntimeException exception = new("boom");
                    DynValue handler = context.Script.Globals.Get("decorator");
                    context.PerformMessageDecorationBeforeUnwind(handler, exception);
                    return DynValue.NewString(exception.DecoratedMessage);
                }
            );
            script.Globals["decorateMessage"] = callback;

            DynValue result = script.DoString("return decorateMessage()");
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
            CallbackFunction callback = new((_, _) => DynValue.Nil);
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
            DynValue func = DynValue.NewCallback(
                (_, _) => DynValue.NewYieldReq(Array.Empty<DynValue>())
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
            DynValue func = DynValue.NewCallback(
                (_, _) =>
                    DynValue.NewTailCallReq(
                        new TailCallData
                        {
                            Function = DynValue.NewCallback((_, _) => DynValue.NewNumber(1)),
                            Continuation = new CallbackFunction((_, _) => DynValue.Nil),
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
            DynValue inner = DynValue.NewCallback(
                (_, args) => DynValue.NewNumber(args[0].Number + 24)
            );
            DynValue func = DynValue.NewCallback(
                (_, _) =>
                    DynValue.NewTailCallReq(
                        new TailCallData
                        {
                            Function = inner,
                            Args = new[] { DynValue.NewNumber(18) },
                        }
                    )
            );

            DynValue result = context.Call(func);
            await Assert.That(result.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FixedCallOverloadsInvokeLuaFunctions(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            script.DoString("function add4(a, b, c, d) return a + b + c + d end");
            DynValue add4 = script.Globals.Get("add4");
            DynValue callback = DynValue.NewCallbackView(
                (context, _) =>
                    context.Call(
                        add4,
                        DynValue.NewNumber(10),
                        DynValue.NewNumber(20),
                        DynValue.NewNumber(30),
                        DynValue.NewNumber(40)
                    )
            );
            script.Globals["callAdd4"] = callback;

            DynValue result = script.DoString("return callAdd4()");
            await Assert.That(result.Number).IsEqualTo(100d);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FixedCallOverloadsInvokeCallbackViews(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue inner = DynValue.NewCallbackView(
                (_, args) =>
                    DynValue.NewNumber(
                        args.Count
                            + args[0].Number
                            + args[1].Number
                            + args[2].Number
                            + args[3].Number
                    )
            );
            DynValue callback = DynValue.NewCallbackView(
                (context, _) =>
                    context.Call(
                        inner,
                        DynValue.NewNumber(10),
                        DynValue.NewNumber(20),
                        DynValue.NewNumber(30),
                        DynValue.NewNumber(40)
                    )
            );
            script.Globals["callInner"] = callback;

            DynValue result = script.DoString("return callInner()");
            await Assert.That(result.Number).IsEqualTo(104d);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FixedCallOverloadsPreserveLegacyCallbackArgumentsWithFixedStorageSpan(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            DynValue inner = DynValue.NewCallback(
                (_, args) =>
                {
                    bool success = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    DynValue[] copied = new DynValue[args.Count];
                    int copiedCount = args.CopyTo(copied);
                    return DynValue.NewTuple(
                        DynValue.NewBoolean(success),
                        DynValue.NewNumber(span.Length),
                        DynValue.NewNumber(copiedCount),
                        copied[0],
                        copied[1],
                        copied[2]
                    );
                }
            );
            DynValue callback = DynValue.NewCallbackView(
                (context, _) =>
                    context.Call(
                        inner,
                        DynValue.NewNumber(10),
                        DynValue.NewNumber(20),
                        DynValue.NewNumber(30)
                    )
            );
            script.Globals["callInner"] = callback;

            DynValue result = script.DoString("return callInner()");
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Number).IsEqualTo(3d);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(3d);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(10d);
            await Assert.That(result.Tuple[4].Number).IsEqualTo(20d);
            await Assert.That(result.Tuple[5].Number).IsEqualTo(30d);
        }

        [global::TUnit.Core.Test]
        public async Task FixedCallOverloadsAvoidLegacyCallbackArgumentArrayAllocation()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue noArgCallback = DynValue.NewCallback(
                (_, args) =>
                {
                    if (args.Count != 0)
                    {
                        throw new InvalidOperationException(
                            "No-argument allocation probe received arguments."
                        );
                    }

                    return DynValue.Nil;
                }
            );
            DynValue threeArgCallback = DynValue.NewCallback(
                (_, args) =>
                {
                    if (
                        args.Count != 3
                        || args[0].Number != 1d
                        || args[1].Number != 2d
                        || args[2].Number != 3d
                    )
                    {
                        throw new InvalidOperationException(
                            "Fixed-argument allocation probe received unexpected arguments."
                        );
                    }

                    return DynValue.Nil;
                }
            );
            DynValue spanProbeCallback = DynValue.NewCallback(
                (_, args) =>
                {
                    bool hasSpan = args.TryGetSpan(out ReadOnlySpan<DynValue> span);
                    if (!hasSpan || span.Length != 3)
                    {
                        throw new InvalidOperationException(
                            "Fixed-argument allocation probe did not expose the expected span."
                        );
                    }

                    return DynValue.Nil;
                }
            );
            DynValue first = DynValue.NewNumber(1);
            DynValue second = DynValue.NewNumber(2);
            DynValue third = DynValue.NewNumber(3);

            MeasureNoArgumentContextCallAllocations(context, noArgCallback, iterations: 8);
            MeasureFixedThreeArgumentContextCallAllocations(
                context,
                threeArgCallback,
                first,
                second,
                third,
                iterations: 8
            );
            MeasureFixedThreeArgumentContextCallAllocations(
                context,
                spanProbeCallback,
                first,
                second,
                third,
                iterations: 8
            );

            int iterations = 1_024;
            long noArgumentAllocated = MeasureNoArgumentContextCallAllocations(
                context,
                noArgCallback,
                iterations
            );
            long fixedArgumentAllocated = MeasureFixedThreeArgumentContextCallAllocations(
                context,
                threeArgCallback,
                first,
                second,
                third,
                iterations
            );
            long spanProbeAllocated = MeasureFixedThreeArgumentContextCallAllocations(
                context,
                spanProbeCallback,
                first,
                second,
                third,
                iterations
            );
            long extraBytesPerCall = (fixedArgumentAllocated - noArgumentAllocated) / iterations;
            long spanProbeExtraBytesPerCall =
                (spanProbeAllocated - noArgumentAllocated) / iterations;

            await Assert.That(extraBytesPerCall).IsLessThan(16L);
            await Assert.That(spanProbeExtraBytesPerCall).IsLessThan(16L);
        }

        [global::TUnit.Core.Test]
        public async Task FixedCallOverloadsPreserveLegacyCallbackExpansionSemantics()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue inspect = DynValue.NewCallback(
                (_, args) =>
                    DynValue.NewTuple(DynValue.NewNumber(args.Count), args[0], args[1], args[2])
            );
            DynValue countVoid = DynValue.NewCallback((_, args) => DynValue.NewNumber(args.Count));

            DynValue expanded = context.Call(
                inspect,
                null,
                DynValue.NewTuple(DynValue.NewNumber(2), null)
            );
            DynValue voidTrimmed = context.Call(countVoid, DynValue.NewNumber(1), DynValue.Void);

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
        public async Task FixedCallOverloadsPreserveLegacyCallbackArity(int arity)
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue inspect = DynValue.NewCallback(
                (_, args) =>
                {
                    double sum = 0d;
                    for (int i = 0; i < args.Count; i++)
                    {
                        sum += args[i].Number;
                    }

                    return DynValue.NewTuple(
                        DynValue.NewNumber(args.Count),
                        DynValue.NewNumber(sum)
                    );
                }
            );

            DynValue result = arity switch
            {
                1 => context.Call(inspect, DynValue.NewNumber(1)),
                2 => context.Call(inspect, DynValue.NewNumber(1), DynValue.NewNumber(2)),
                3 => context.Call(
                    inspect,
                    DynValue.NewNumber(1),
                    DynValue.NewNumber(2),
                    DynValue.NewNumber(3)
                ),
                4 => context.Call(
                    inspect,
                    DynValue.NewNumber(1),
                    DynValue.NewNumber(2),
                    DynValue.NewNumber(3),
                    DynValue.NewNumber(4)
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Number).IsEqualTo((double)arity);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(arity * (arity + 1) / 2d);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(2)]
        [global::TUnit.Core.Arguments(3)]
        [global::TUnit.Core.Arguments(4)]
        public async Task FixedCallOverloadsPreserveLegacyCallbackSpecialArguments(int arity)
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue inspect = DynValue.NewCallback(
                (_, args) =>
                {
                    double nilCount = 0d;
                    double sum = 0d;

                    for (int i = 0; i < args.Count; i++)
                    {
                        DynValue arg = args[i];
                        if (arg.Type == DataType.Nil)
                        {
                            nilCount++;
                        }
                        else
                        {
                            sum += arg.Number;
                        }
                    }

                    return DynValue.NewTuple(
                        DynValue.NewNumber(args.Count),
                        DynValue.NewNumber(nilCount),
                        DynValue.NewNumber(sum)
                    );
                }
            );

            DynValue result = arity switch
            {
                1 => context.Call(inspect, (DynValue)null),
                2 => context.Call(inspect, DynValue.NewNumber(1), DynValue.Void),
                3 => context.Call(
                    inspect,
                    DynValue.NewNumber(1),
                    DynValue.NewNumber(2),
                    DynValue.NewTuple(DynValue.NewNumber(3), null)
                ),
                4 => context.Call(
                    inspect,
                    null,
                    DynValue.NewNumber(2),
                    DynValue.NewNumber(3),
                    DynValue.NewTuple(DynValue.NewNumber(4), null)
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };

            double expectedCount = arity switch
            {
                1 => 1d,
                2 => 1d,
                3 => 4d,
                4 => 5d,
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };
            double expectedNilCount = arity switch
            {
                1 => 1d,
                2 => 0d,
                3 => 1d,
                4 => 2d,
                _ => throw new ArgumentOutOfRangeException(nameof(arity)),
            };
            double expectedSum = arity switch
            {
                1 => 0d,
                2 => 1d,
                3 => 6d,
                4 => 9d,
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
            DynValue func = DynValue.NewCallbackView(
                (_, _) =>
                    DynValue.NewTailCallReq(
                        new TailCallData
                        {
                            Function = DynValue.NewCallback((_, _) => DynValue.NewNumber(1)),
                            Continuation = new CallbackFunction((_, _) => DynValue.Nil),
                        }
                    )
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(func, DynValue.NewNumber(1))
            );
            await Assert.That(exception.Message).Contains("cannot be called directly");
        }

        [global::TUnit.Core.Test]
        public async Task CallUsesCallMetamethod()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table target = new(script);
            Table meta = new(script);
            meta.Set(
                "__call",
                DynValue.NewCallback(
                    (_, args) =>
                        DynValue.NewTuple(
                            DynValue.NewBoolean(args.Count == 2),
                            DynValue.NewBoolean(ReferenceEquals(args[0].Table, target)),
                            args[1]
                        )
                )
            );
            target.MetaTable = meta;

            DynValue result = context.Call(DynValue.NewTable(target), DynValue.NewNumber(1));
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Boolean).IsTrue();
            await Assert.That(result.Tuple[2].Number).IsEqualTo(1d);
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
            targetMeta.Set("__call", DynValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                DynValue.NewCallback((_, _) => DynValue.NewString("unexpected"))
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(DynValue.NewTable(target), DynValue.NewNumber(9))
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
            targetMeta.Set("__call", DynValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                DynValue.NewCallback(
                    (_, args) =>
                        DynValue.NewTuple(
                            DynValue.NewBoolean(args.Count == 3),
                            DynValue.NewBoolean(ReferenceEquals(args[0].Table, proxy)),
                            DynValue.NewBoolean(ReferenceEquals(args[1].Table, target)),
                            args[2]
                        )
                )
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            DynValue result = context.Call(DynValue.NewTable(target), DynValue.NewNumber(9));

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Boolean).IsTrue();
            await Assert.That(result.Tuple[2].Boolean).IsTrue();
            await Assert.That(result.Tuple[3].Number).IsEqualTo(9d);
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
            targetMeta.Set("__call", DynValue.NewTable(proxy));
            proxyMeta.Set(
                "__call",
                DynValue.NewCallback(
                    (_, args) =>
                        DynValue.NewTuple(
                            DynValue.NewBoolean(args.Count == 3),
                            DynValue.NewBoolean(ReferenceEquals(args[0].Table, proxy)),
                            DynValue.NewBoolean(ReferenceEquals(args[1].Table, target)),
                            args[2]
                        )
                )
            );
            target.MetaTable = targetMeta;
            proxy.MetaTable = proxyMeta;

            DynValue result = context.Call(DynValue.NewTable(target), DynValue.NewNumber(9));

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

            DynValue nil = context.EvaluateSymbol(null);
            await Assert.That(nil).IsEqualTo(DynValue.Nil);
        }

        [global::TUnit.Core.Test]
        public async Task GetMetatableThrowsWhenValueIsNull()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                context.GetMetatable(null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("value");
        }

        [global::TUnit.Core.Test]
        public async Task GetMetamethodThrowsWhenArgumentsNull()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue value = DynValue.NewNumber(1);

            ArgumentNullException valueException = ExpectException<ArgumentNullException>(() =>
                context.GetMetamethod(null, "__call")
            );
            ArgumentNullException methodException = ExpectException<ArgumentNullException>(() =>
                context.GetMetamethod(value, null)
            );

            await Assert.That(valueException.ParamName).IsEqualTo("value");
            await Assert.That(methodException.ParamName).IsEqualTo("metamethod");
        }

        [global::TUnit.Core.Test]
        public async Task GetBinaryMetamethodValidatesArguments()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue operand = DynValue.NewNumber(1);

            ArgumentNullException op1Exception = ExpectException<ArgumentNullException>(() =>
                context.GetBinaryMetamethod(null, operand, "__add")
            );
            ArgumentNullException op2Exception = ExpectException<ArgumentNullException>(() =>
                context.GetBinaryMetamethod(operand, null, "__add")
            );
            ArgumentNullException eventException = ExpectException<ArgumentNullException>(() =>
                context.GetBinaryMetamethod(operand, operand, null)
            );

            await Assert.That(op1Exception.ParamName).IsEqualTo("op1");
            await Assert.That(op2Exception.ParamName).IsEqualTo("op2");
            await Assert.That(eventException.ParamName).IsEqualTo("eventName");
        }

        [global::TUnit.Core.Test]
        public async Task EmulateClassicCallValidatesArguments()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(Array.Empty<DynValue>(), false);

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

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                context.Call(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("func");
        }

        [global::TUnit.Core.Test]
        public async Task GetMetamethodTailCallReturnsNullWhenMissing()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue target = DynValue.NewTable(new Table(script));

            DynValue tail = context.GetMetamethodTailCall(target, "__call");
            await Assert.That(tail).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task PerformMessageDecorationDefaultsToOriginal()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            ScriptRuntimeException exception = new("boom");

            context.PerformMessageDecorationBeforeUnwind(null, exception);
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
            DynValue callback = DynValue.NewCallback(
                (context, _) => DynValue.NewBoolean(context.IsYieldable())
            );
            script.Globals["yieldState"] = callback;

            DynValue result = script.DoString("return yieldState()");
            await Assert.That(result.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task IsYieldableReturnsTrueInsideCoroutine(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue callback = DynValue.NewCallback(
                (context, _) => DynValue.NewBoolean(context.IsYieldable())
            );
            script.Globals["yieldState"] = callback;
            script.DoString("function coroutineProbe() return yieldState() end");

            DynValue coroutineHandle = script.CreateCoroutine(script.Globals.Get("coroutineProbe"));
            DynValue resumeResult = coroutineHandle.Coroutine.Resume();

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
                meta.Set("__call", DynValue.NewTable(next));
                current.MetaTable = meta;
                current = next;
            }

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(DynValue.NewTable(root))
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
                context.Call(DynValue.NewTable(target))
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
            meta.Set("__call", DynValue.Nil);
            target.MetaTable = meta;

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(DynValue.NewTable(target))
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

        private static long MeasureNoArgumentContextCallAllocations(
            ScriptExecutionContext context,
            DynValue callback,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                DynValue result = context.Call(callback);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "No-argument context call allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureFixedThreeArgumentContextCallAllocations(
            ScriptExecutionContext context,
            DynValue callback,
            DynValue first,
            DynValue second,
            DynValue third,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                DynValue result = context.Call(callback, first, second, third);
                if (result.Type != DataType.Nil)
                {
                    throw new InvalidOperationException(
                        "Fixed-argument context call allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static DynValue LastTailCall;
    }
}
