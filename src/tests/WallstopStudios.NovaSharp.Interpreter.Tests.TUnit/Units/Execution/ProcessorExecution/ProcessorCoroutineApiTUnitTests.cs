namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    public sealed class ProcessorCoroutineApiTUnitTests
    {
        private static readonly int[] YieldedValues = { 1, 2, 3 };

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeAfterCompletionThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.DoString("return function() return 1 end");
            LuaValue coroutine = script.CreateCoroutineValue(function);

            LuaValue first = coroutine.Coroutine.Resume();
            await Assert.That(first.Number).IsEqualTo(1d);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutine.Coroutine.Resume()
            );
            await Assert.That(exception.Message).Contains("cannot resume dead coroutine");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AsTypedEnumerableIteratesAllResults(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.DoString(
                "return function() coroutine.yield(1) coroutine.yield(2) return 3 end"
            );
            LuaValue coroutine = script.CreateCoroutineValue(function);

            List<int> results = new();
            foreach (LuaValue value in coroutine.Coroutine.AsTypedEnumerable())
            {
                results.Add((int)value.Number);
            }

            await Assert.That(results.SequenceEqual(YieldedValues)).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AsTypedEnumerableThrowsForClrCallbacks(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil);
            LuaValue coroutine = script.CreateCoroutineValue(callback);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
            {
                foreach (LuaValue _ in coroutine.Coroutine.AsTypedEnumerable())
                {
                    // Enumeration should never succeed for CLR callbacks.
                }
            });
            await Assert.That(exception.Message).Contains("Only non-CLR coroutines");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AsEnumerableReturnsObjects(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.DoString(
                "return function() coroutine.yield(10) coroutine.yield(20) return 30 end"
            );
            LuaValue coroutine = script.CreateCoroutineValue(function);

            List<object> results = coroutine.Coroutine.AsEnumerable().ToList();

            // Numeric values may come back as long (integer) or double depending on representation
            await Assert.That(results.Count).IsEqualTo(3);
            await Assert
                .That(Convert.ToDouble(results[0], CultureInfo.InvariantCulture))
                .IsEqualTo(10d);
            await Assert
                .That(Convert.ToDouble(results[1], CultureInfo.InvariantCulture))
                .IsEqualTo(20d);
            await Assert
                .That(Convert.ToDouble(results[2], CultureInfo.InvariantCulture))
                .IsEqualTo(30d);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AsEnumerableOfTReturnsTypedScalars(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.DoString(
                "return function() coroutine.yield(1) coroutine.yield(2) return 3 end"
            );
            LuaValue coroutine = script.CreateCoroutineValue(function);

            List<int> results = coroutine.Coroutine.AsEnumerable<int>().ToList();
            await Assert.That(results.SequenceEqual(YieldedValues)).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AsUnityCoroutineYieldsNullPerIteration(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.DoString(
                "return function() coroutine.yield('a') coroutine.yield('b') return 'c' end"
            );
            LuaValue coroutine = script.CreateCoroutineValue(function);

            System.Collections.IEnumerator unityCoroutine = coroutine.Coroutine.AsUnityCoroutine();
            List<object> yielded = new();

            while (unityCoroutine.MoveNext())
            {
                yielded.Add(unityCoroutine.Current);
            }

            await Assert.That(yielded.Count).IsEqualTo(3);
            await Assert.That(yielded.TrueForAll(value => value == null)).IsTrue();
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MarkClrCallbackAsDeadTransitionsType(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil);
            LuaValue coroutine = script.CreateCoroutineValue(callback);

            coroutine.Coroutine.MarkClrCallbackAsDead();

            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MarkClrCallbackAsDeadThrowsWhenCoroutineNotCallback(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue function = script.DoString("return function() return 1 end");
            LuaValue coroutine = script.CreateCoroutineValue(function);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                coroutine.Coroutine.MarkClrCallbackAsDead()
            );
            await Assert.That(exception.Message).Contains("CoroutineType.ClrCallback");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StateTransitionsFollowCoroutineLifecycle(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.DoString(
                "return function() coroutine.yield(1) coroutine.yield(2) end"
            );
            LuaValue coroutine = script.CreateCoroutineValue(function);

            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.NotStarted);

            LuaValue first = coroutine.Coroutine.Resume();
            await Assert.That(first.Number).IsEqualTo(1d);
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Suspended);

            coroutine.Coroutine.Resume();
            coroutine.Coroutine.Resume();

            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeClrCallbackExecutesAndMarksDead(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            LuaValue callback = LuaValue.NewCallback(
                (ctx, args) =>
                {
                    return args.Count > 0 ? args[0] : LuaValue.NewNumber(99);
                }
            );
            LuaValue coroutine = script.CreateCoroutineValue(callback);
            coroutine.Coroutine.OwnerScript = script;

            LuaValue payload = LuaValue.NewString("payload");
            LuaValue result = coroutine.Coroutine.Resume(context, payload);

            await Assert.That(result.String).IsEqualTo("payload");
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeClrCallbackTwiceThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            LuaValue callback = LuaValue.NewCallback((ctx, _) => LuaValue.NewNumber(1));
            LuaValue coroutine = script.CreateCoroutineValue(callback);
            coroutine.Coroutine.OwnerScript = script;

            LuaValue first = coroutine.Coroutine.Resume(context);
            await Assert.That(first.Number).IsEqualTo(1d);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutine.Coroutine.Resume(context)
            );
            await Assert.That(exception.Message).Contains("cannot resume dead coroutine");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithExplicitContextUsesDefaultArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue function = script.DoString("return function() return 5 end");
            LuaValue coroutine = script.CreateCoroutineValue(function);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            LuaValue result = coroutine.Coroutine.Resume(context);

            await Assert.That(result.Number).IsEqualTo(5d);
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        [ScriptGlobalOptionsIsolation]
        public async Task ResumeWithObjectArgumentsConvertsValues(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.DoString("return function(a, b) return a + b end");
            LuaValue coroutine = script.CreateCoroutineValue(function);

            LuaValue result = coroutine.Coroutine.Resume(40, 2);

            await Assert.That(result.Number).IsEqualTo(42d);

            using ScriptCustomConvertersScope converterScope = ScriptCustomConvertersScope.Clear(
                registry =>
                {
                    registry.SetClrToScriptCustomConversion<int>(
                        (_, value) => LuaValue.NewString("custom-int:" + value)
                    );
                    registry.SetClrToScriptCustomConversion<string>(
                        (_, value) => LuaValue.NewString("custom-string:" + value)
                    );
                }
            );
            LuaValue identity = script.DoString("return function(value) return value end");
            LuaValue integerCoroutine = script.CreateCoroutineValue(identity);
            LuaValue stringCoroutine = script.CreateCoroutineValue(identity);

            LuaValue convertedInteger = integerCoroutine.Coroutine.Resume(42);
            LuaValue convertedString = stringCoroutine.Coroutine.Resume("value");

            await Assert.That(convertedInteger.String).IsEqualTo("custom-int:42");
            await Assert.That(convertedString.String).IsEqualTo("custom-string:value");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithContextObjectArgumentsConvertsValues(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue function = script.DoString("return function(a, b) return a + b end");
            LuaValue coroutine = script.CreateCoroutineValue(function);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            LuaValue result = coroutine.Coroutine.Resume(context, 30, 12);

            await Assert.That(result.Number).IsEqualTo(42d);
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithDynValueSpanUsesSliceAndNormalizesNull(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue function = script.DoString(
                "return function(a, b, c) if a ~= nil then return -1 end return b + c end"
            );
            LuaValue coroutine = script.CreateCoroutineValue(function);
            LuaValue[] args =
            {
                LuaValue.NewNumber(-1),
                LuaValue.Nil,
                LuaValue.NewNumber(40),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(-1),
            };

            LuaValue result = ResumeSpan(coroutine.Coroutine, args, start: 1, length: 3);

            await Assert.That(result.Number).IsEqualTo(42d);
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SuspendedCoroutineReceivesDynValueSpanResumeArguments(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue function = script.DoString(
                @"
                return function()
                    local a, b, c = coroutine.yield('ready')
                    if a ~= nil then
                        return -1
                    end
                    return b + c
                end"
            );
            LuaValue coroutine = script.CreateCoroutineValue(function);
            LuaValue yielded = coroutine.Coroutine.Resume();
            LuaValue[] args =
            {
                LuaValue.NewNumber(-1),
                LuaValue.Nil,
                LuaValue.NewNumber(40),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(-1),
            };

            LuaValue result = ResumeSpan(coroutine.Coroutine, args, start: 1, length: 3);

            await Assert.That(yielded.String).IsEqualTo("ready");
            await Assert.That(result.Number).IsEqualTo(42d);
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithDynValueSpanRejectsForeignArguments(
            LuaCompatibilityVersion version
        )
        {
            Script owningScript = new(version);
            LuaValue function = owningScript.DoString("return function(value) return value end");
            LuaValue coroutine = owningScript.CreateCoroutineValue(function);
            Script foreignScript = new(version);
            LuaValue foreignResource = LuaValue.NewTable(foreignScript);
            LuaValue[] args = { LuaValue.Nil, foreignResource };

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                ResumeSpan(coroutine.Coroutine, args, start: 1, length: 1)
            );

            await Assert.That(exception.Message).Contains("different scripts");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithDynValueSpanOnClrCallbackRejectsBeforeOwnership()
        {
            Script script = new();
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil);
            LuaValue coroutine = script.CreateCoroutineValue(callback);
            Script foreignScript = new();
            LuaValue foreignResource = LuaValue.NewTable(foreignScript);
            LuaValue[] args = { foreignResource };

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                ResumeSpan(coroutine.Coroutine, args, start: 0, length: 1)
            );

            await Assert.That(exception.Message).Contains("Only non-CLR coroutines");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithContextDynValueSpanInvokesArgumentViewCallback(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            bool sawExpectedSpan = false;
            LuaValue callback = LuaValue.NewCallbackView(
                (_, args) =>
                {
                    bool hasSpan = args.TryGetSpan(out ReadOnlySpan<LuaValue> span);
                    sawExpectedSpan =
                        hasSpan
                        && span.Length == 2
                        && span[0].Number == 20d
                        && span[1].Number == 22d;
                    return LuaValue.NewBoolean(sawExpectedSpan);
                }
            );
            LuaValue coroutine = script.CreateCoroutineValue(callback);
            coroutine.Coroutine.OwnerScript = script;
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            LuaValue[] args =
            {
                LuaValue.NewNumber(-1),
                LuaValue.NewNumber(20),
                LuaValue.NewNumber(22),
                LuaValue.NewNumber(-1),
            };

            LuaValue result = ResumeSpan(coroutine.Coroutine, context, args, start: 1, length: 2);

            await Assert.That(result.Boolean).IsTrue();
            await Assert.That(sawExpectedSpan).IsTrue();
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithContextDynValueSpanInvokesLegacyCallback(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue callback = LuaValue.NewCallback(
                (_, args) => LuaValue.NewNumber(args[0].Number + args[1].Number)
            );
            LuaValue coroutine = script.CreateCoroutineValue(callback);
            coroutine.Coroutine.OwnerScript = script;
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            LuaValue[] args =
            {
                LuaValue.NewNumber(-1),
                LuaValue.NewNumber(30),
                LuaValue.NewNumber(12),
                LuaValue.NewNumber(-1),
            };

            LuaValue result = ResumeSpan(coroutine.Coroutine, context, args, start: 1, length: 2);

            await Assert.That(result.Number).IsEqualTo(42d);
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithObjectArgumentsOnClrCallbackThrows(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil);
            LuaValue coroutine = script.CreateCoroutineValue(callback);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                coroutine.Coroutine.Resume("value")
            );
            await Assert.That(exception.Message).Contains("Only non-CLR coroutines");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithObjectArgumentsOnClrCallbackRejectsBeforeConversion(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil);
            LuaValue coroutine = script.CreateCoroutineValue(callback);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                coroutine.Coroutine.Resume(new UnregisteredHostObject())
            );
            await Assert.That(exception.Message).Contains("Only non-CLR coroutines");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithDynValueArgumentsThrowsWhenNull(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.DoString("return function() return 1 end");
            LuaValue coroutine = script.CreateCoroutineValue(function);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                coroutine.Coroutine.Resume((LuaValue[])null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithDynValueArgumentsThrowsForClrCallbacks(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil);
            LuaValue coroutine = script.CreateCoroutineValue(callback);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                coroutine.Coroutine.Resume(Array.Empty<LuaValue>())
            );
            await Assert.That(exception.Message).Contains("Only non-CLR coroutines");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithDynValueArrayOnClrCallbackRejectsBeforeOwnership()
        {
            Script script = new();
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil);
            LuaValue coroutine = script.CreateCoroutineValue(callback);
            Script foreignScript = new();
            LuaValue foreignResource = LuaValue.NewTable(foreignScript);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                coroutine.Coroutine.Resume(new[] { foreignResource })
            );

            await Assert.That(exception.Message).Contains("Only non-CLR coroutines");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(2)]
        [global::TUnit.Core.Arguments(3)]
        [global::TUnit.Core.Arguments(4)]
        [global::TUnit.Core.Arguments(5)]
        public async Task FixedDynValueResumeOnClrCallbackRejectsBeforeOwnership(int argumentCount)
        {
            Script script = new();
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil);
            LuaValue coroutine = script.CreateCoroutineValue(callback);
            Script foreignScript = new();
            LuaValue foreignResource = LuaValue.NewTable(foreignScript);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                ResumeFixedDynValueArguments(coroutine.Coroutine, foreignResource, argumentCount)
            );

            await Assert.That(exception.Message).Contains("Only non-CLR coroutines");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithContextArgsThrowsWhenContextNull(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue function = script.DoString("return function() return 1 end");
            LuaValue coroutine = script.CreateCoroutineValue(function);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                coroutine.Coroutine.Resume(null, Array.Empty<LuaValue>())
            );
            await Assert.That(exception.ParamName).IsEqualTo("context");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithContextArgsThrowsWhenArgsNull(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.DoString("return function() return 1 end");
            LuaValue coroutine = script.CreateCoroutineValue(function);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                coroutine.Coroutine.Resume(context, (LuaValue[])null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithObjectArgsThrowsWhenArgsNull(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.DoString("return function() return 1 end");
            LuaValue coroutine = script.CreateCoroutineValue(function);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                coroutine.Coroutine.Resume((object[])null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithContextObjectArgsThrowsWhenArgsNull(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue function = script.DoString("return function() return 1 end");
            LuaValue coroutine = script.CreateCoroutineValue(function);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                coroutine.Coroutine.Resume(context, (object[])null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AutoYieldCounterForcesSuspendUntilResumed(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.DoString("return function() return 42 end");
            LuaValue coroutine = script.CreateCoroutineValue(function);
            coroutine.Coroutine.AutoYieldCounter = 1;

            LuaValue forced = coroutine.Coroutine.Resume();
            await Assert.That(forced.Type).IsEqualTo(DataType.YieldRequest);
            await Assert.That(forced.YieldRequest.Forced).IsTrue();
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.ForceSuspended);

            coroutine.Coroutine.AutoYieldCounter = 0;
            LuaValue finalResult = coroutine.Coroutine.Resume();

            await Assert.That(finalResult.Number).IsEqualTo(42d);
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithContextFromDifferentScriptThrows(
            LuaCompatibilityVersion version
        )
        {
            Script owningScript = new();
            LuaValue function = owningScript.DoString("return function() return 1 end");
            LuaValue coroutine = owningScript.CreateCoroutineValue(function);

            Script foreignScript = new();
            ScriptExecutionContext foreignContext = TestHelpers.CreateExecutionContext(
                foreignScript
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutine.Coroutine.Resume(foreignContext)
            );
            await Assert.That(exception.Message).Contains("different scripts");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeObjectArgumentsWithContextFromDifferentScriptThrows(
            LuaCompatibilityVersion version
        )
        {
            Script owningScript = new(version);
            LuaValue function = owningScript.DoString("return function(value) return value end");
            LuaValue coroutine = owningScript.CreateCoroutineValue(function);

            Script foreignScript = new(version);
            ScriptExecutionContext foreignContext = TestHelpers.CreateExecutionContext(
                foreignScript
            );
            object[] args = { 1d };

            ScriptRuntimeException arrayException = ExpectException<ScriptRuntimeException>(() =>
                coroutine.Coroutine.ResumeObjectArguments(foreignContext, args)
            );
            ScriptRuntimeException spanException = ExpectException<ScriptRuntimeException>(() =>
                coroutine.Coroutine.ResumeObjectArguments(foreignContext, args.AsSpan())
            );

            await Assert.That(arrayException.Message).Contains("different scripts");
            await Assert.That(spanException.Message).Contains("different scripts");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithArgumentsFromDifferentScriptThrows(
            LuaCompatibilityVersion version
        )
        {
            Script owningScript = new();
            LuaValue function = owningScript.DoString("return function(value) return value end");
            LuaValue coroutine = owningScript.CreateCoroutineValue(function);

            Script foreignScript = new();
            LuaValue foreignResource = LuaValue.NewTable(foreignScript);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutine.Coroutine.Resume(foreignResource)
            );
            await Assert.That(exception.Message).Contains("different scripts");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeWithFourthArgumentFromDifferentScriptThrows(
            LuaCompatibilityVersion version
        )
        {
            Script owningScript = new();
            LuaValue function = owningScript.DoString("return function(a, b, c, d) return d end");
            LuaValue coroutine = owningScript.CreateCoroutineValue(function);

            Script foreignScript = new();
            LuaValue foreignResource = LuaValue.NewTable(foreignScript);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutine.Coroutine.ResumeValues(
                    LuaValue.Nil,
                    LuaValue.Nil,
                    LuaValue.Nil,
                    foreignResource
                )
            );
            await Assert.That(exception.Message).Contains("different scripts");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AutoYieldCounterProxiesProcessorValue(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue function = script.DoString("return function() coroutine.yield(1) end");
            LuaValue coroutine = script.CreateCoroutineValue(function);

            coroutine.Coroutine.AutoYieldCounter = 42;

            await Assert.That(coroutine.Coroutine.AutoYieldCounter).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetProcessorForTestsThrowsForClrCallbacks(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil);
            LuaValue coroutine = script.CreateCoroutineValue(callback);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                coroutine.Coroutine.GetProcessorForTests()
            );
            await Assert.That(exception.Message).Contains("CLR callback");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ForceStateForTestsThrowsForClrCallbacks(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil);
            LuaValue coroutine = script.CreateCoroutineValue(callback);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                coroutine.Coroutine.ForceStateForTests(CoroutineState.Suspended)
            );
            await Assert.That(exception.Message).Contains("CLR callback");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseReturnsTrueForClrCallbacks(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.Nil);
            LuaValue coroutine = script.CreateCoroutineValue(callback);

            LuaValue result = coroutine.Coroutine.Close();

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(result.Boolean).IsTrue();
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.NotStarted);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task GetStackTraceUsesSuspendedLocationWhenNotRunning(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            script.Options.DebugPrint = _ => { };
            LuaValue function = script.DoString(
                @"
                return function()
                    local function inner()
                        coroutine.yield('pause')
                    end

                    inner()
                end
            "
            );

            LuaValue coroutine = script.CreateCoroutineValue(function);
            LuaValue yielded = coroutine.Coroutine.Resume();
            await Assert.That(yielded.ToScalar().ToObject<string>()).IsEqualTo("pause");

            WatchItem[] stack = coroutine.Coroutine.GetStackTrace(0, SourceRef.GetClrLocation());
            await Assert.That(stack.Length > 0).IsTrue();
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

        private static LuaValue ResumeFixedDynValueArguments(
            Coroutine coroutine,
            LuaValue foreignResource,
            int argumentCount
        )
        {
            return argumentCount switch
            {
                1 => coroutine.ResumeValues(foreignResource),
                2 => coroutine.ResumeValues(LuaValue.Nil, foreignResource),
                3 => coroutine.ResumeValues(LuaValue.Nil, LuaValue.Nil, foreignResource),
                4 => coroutine.ResumeValues(
                    LuaValue.Nil,
                    LuaValue.Nil,
                    LuaValue.Nil,
                    foreignResource
                ),
                5 => coroutine.ResumeValues(
                    LuaValue.Nil,
                    LuaValue.Nil,
                    LuaValue.Nil,
                    LuaValue.Nil,
                    foreignResource
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(argumentCount)),
            };
        }

        private static LuaValue ResumeSpan(
            Coroutine coroutine,
            LuaValue[] args,
            int start,
            int length
        )
        {
            return coroutine.Resume(args.AsSpan(start, length));
        }

        private static LuaValue ResumeSpan(
            Coroutine coroutine,
            ScriptExecutionContext context,
            LuaValue[] args,
            int start,
            int length
        )
        {
            return coroutine.Resume(context, args.AsSpan(start, length));
        }

        private sealed class UnregisteredHostObject { }
    }
}
