namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;

    public sealed class ProcessorCoroutineApiTUnitTests
    {
        private static readonly int[] YieldedValues = { 1, 2, 3 };

        [global::TUnit.Core.Test]
        public async Task ResumeAfterCompletionThrows()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);

            DynValue first = coroutine.Coroutine.Resume();
            await Assert.That(first.Number).IsEqualTo(1d);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutine.Coroutine.Resume()
            );
            await Assert.That(exception.Message).Contains("cannot resume dead coroutine");
        }

        [global::TUnit.Core.Test]
        public async Task AsTypedEnumerableIteratesAllResults()
        {
            Script script = new();
            DynValue function = script.DoString(
                "return function() coroutine.yield(1) coroutine.yield(2) return 3 end"
            );
            DynValue coroutine = script.CreateCoroutine(function);

            List<int> results = new();
            foreach (DynValue value in coroutine.Coroutine.AsTypedEnumerable())
            {
                results.Add((int)value.Number);
            }

            await Assert.That(results.SequenceEqual(YieldedValues)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task AsTypedEnumerableThrowsForClrCallbacks()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
            {
                foreach (DynValue _ in coroutine.Coroutine.AsTypedEnumerable())
                {
                    // Enumeration should never succeed for CLR callbacks.
                }
            });
            await Assert.That(exception.Message).Contains("Only non-CLR coroutines");
        }

        [global::TUnit.Core.Test]
        public async Task AsEnumerableReturnsObjects()
        {
            Script script = new();
            DynValue function = script.DoString(
                "return function() coroutine.yield(10) coroutine.yield(20) return 30 end"
            );
            DynValue coroutine = script.CreateCoroutine(function);

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
        public async Task AsEnumerableOfTReturnsTypedScalars()
        {
            Script script = new();
            DynValue function = script.DoString(
                "return function() coroutine.yield(1) coroutine.yield(2) return 3 end"
            );
            DynValue coroutine = script.CreateCoroutine(function);

            List<int> results = coroutine.Coroutine.AsEnumerable<int>().ToList();
            await Assert.That(results.SequenceEqual(YieldedValues)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task AsUnityCoroutineYieldsNullPerIteration()
        {
            Script script = new();
            DynValue function = script.DoString(
                "return function() coroutine.yield('a') coroutine.yield('b') return 'c' end"
            );
            DynValue coroutine = script.CreateCoroutine(function);

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
        public async Task MarkClrCallbackAsDeadTransitionsType()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            coroutine.Coroutine.MarkClrCallbackAsDead();

            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task MarkClrCallbackAsDeadThrowsWhenCoroutineNotCallback()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                coroutine.Coroutine.MarkClrCallbackAsDead()
            );
            await Assert.That(exception.Message).Contains("CoroutineType.ClrCallback");
        }

        [global::TUnit.Core.Test]
        public async Task StateTransitionsFollowCoroutineLifecycle()
        {
            Script script = new();
            DynValue function = script.DoString(
                "return function() coroutine.yield(1) coroutine.yield(2) end"
            );
            DynValue coroutine = script.CreateCoroutine(function);

            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.NotStarted);

            DynValue first = coroutine.Coroutine.Resume();
            await Assert.That(first.Number).IsEqualTo(1d);
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Suspended);

            coroutine.Coroutine.Resume();
            coroutine.Coroutine.Resume();

            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task ResumeClrCallbackExecutesAndMarksDead()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            DynValue callback = DynValue.NewCallback(
                (ctx, args) =>
                {
                    return args.Count > 0 ? args[0] : DynValue.NewNumber(99);
                }
            );
            DynValue coroutine = script.CreateCoroutine(callback);
            coroutine.Coroutine.OwnerScript = script;

            DynValue payload = DynValue.NewString("payload");
            DynValue result = coroutine.Coroutine.Resume(context, payload);

            await Assert.That(result.String).IsEqualTo("payload");
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task ResumeClrCallbackTwiceThrows()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            DynValue callback = DynValue.NewCallback((ctx, _) => DynValue.NewNumber(1));
            DynValue coroutine = script.CreateCoroutine(callback);
            coroutine.Coroutine.OwnerScript = script;

            DynValue first = coroutine.Coroutine.Resume(context);
            await Assert.That(first.Number).IsEqualTo(1d);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutine.Coroutine.Resume(context)
            );
            await Assert.That(exception.Message).Contains("cannot resume dead coroutine");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithExplicitContextUsesDefaultArguments()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 5 end");
            DynValue coroutine = script.CreateCoroutine(function);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue result = coroutine.Coroutine.Resume(context);

            await Assert.That(result.Number).IsEqualTo(5d);
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithObjectArgumentsConvertsValues()
        {
            Script script = new();
            DynValue function = script.DoString("return function(a, b) return a + b end");
            DynValue coroutine = script.CreateCoroutine(function);

            DynValue result = coroutine.Coroutine.Resume(40, 2);

            await Assert.That(result.Number).IsEqualTo(42d);
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithContextObjectArgumentsConvertsValues()
        {
            Script script = new();
            DynValue function = script.DoString("return function(a, b) return a + b end");
            DynValue coroutine = script.CreateCoroutine(function);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            DynValue result = coroutine.Coroutine.Resume(context, 30, 12);

            await Assert.That(result.Number).IsEqualTo(42d);
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithObjectArgumentsOnClrCallbackThrows()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                coroutine.Coroutine.Resume("value")
            );
            await Assert.That(exception.Message).Contains("Only non-CLR coroutines");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithDynValueArgumentsThrowsWhenNull()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                coroutine.Coroutine.Resume((DynValue[])null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithDynValueArgumentsThrowsForClrCallbacks()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                coroutine.Coroutine.Resume(Array.Empty<DynValue>())
            );
            await Assert.That(exception.Message).Contains("Only non-CLR coroutines");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithContextArgsThrowsWhenContextNull()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                coroutine.Coroutine.Resume(null, Array.Empty<DynValue>())
            );
            await Assert.That(exception.ParamName).IsEqualTo("context");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithContextArgsThrowsWhenArgsNull()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                coroutine.Coroutine.Resume(context, null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithObjectArgsThrowsWhenArgsNull()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                coroutine.Coroutine.Resume((object[])null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithContextObjectArgsThrowsWhenArgsNull()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            DynValue coroutine = script.CreateCoroutine(function);
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                coroutine.Coroutine.Resume(context, (object[])null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        public async Task AutoYieldCounterForcesSuspendUntilResumed()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 42 end");
            DynValue coroutine = script.CreateCoroutine(function);
            coroutine.Coroutine.AutoYieldCounter = 1;

            DynValue forced = coroutine.Coroutine.Resume();
            await Assert.That(forced.Type).IsEqualTo(DataType.YieldRequest);
            await Assert.That(forced.YieldRequest.Forced).IsTrue();
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.ForceSuspended);

            coroutine.Coroutine.AutoYieldCounter = 0;
            DynValue finalResult = coroutine.Coroutine.Resume();

            await Assert.That(finalResult.Number).IsEqualTo(42d);
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task ResumeWithContextFromDifferentScriptThrows()
        {
            Script owningScript = new();
            DynValue function = owningScript.DoString("return function() return 1 end");
            DynValue coroutine = owningScript.CreateCoroutine(function);

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
        public async Task ResumeWithArgumentsFromDifferentScriptThrows()
        {
            Script owningScript = new();
            DynValue function = owningScript.DoString("return function(value) return value end");
            DynValue coroutine = owningScript.CreateCoroutine(function);

            Script foreignScript = new();
            DynValue foreignResource = DynValue.NewTable(foreignScript);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutine.Coroutine.Resume(foreignResource)
            );
            await Assert.That(exception.Message).Contains("different scripts");
        }

        [global::TUnit.Core.Test]
        public async Task AutoYieldCounterProxiesProcessorValue()
        {
            Script script = new();
            DynValue function = script.DoString("return function() coroutine.yield(1) end");
            DynValue coroutine = script.CreateCoroutine(function);

            coroutine.Coroutine.AutoYieldCounter = 42;

            await Assert.That(coroutine.Coroutine.AutoYieldCounter).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task GetProcessorForTestsThrowsForClrCallbacks()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                coroutine.Coroutine.GetProcessorForTests()
            );
            await Assert.That(exception.Message).Contains("CLR callback");
        }

        [global::TUnit.Core.Test]
        public async Task ForceStateForTestsThrowsForClrCallbacks()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                coroutine.Coroutine.ForceStateForTests(CoroutineState.Suspended)
            );
            await Assert.That(exception.Message).Contains("CLR callback");
        }

        [global::TUnit.Core.Test]
        public async Task CloseReturnsTrueForClrCallbacks()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.Nil);
            DynValue coroutine = script.CreateCoroutine(callback);

            DynValue result = coroutine.Coroutine.Close();

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(result.Boolean).IsTrue();
            await Assert.That(coroutine.Coroutine.State).IsEqualTo(CoroutineState.NotStarted);
        }

        [global::TUnit.Core.Test]
        public async Task GetStackTraceUsesSuspendedLocationWhenNotRunning()
        {
            Script script = new();
            script.Options.DebugPrint = _ => { };
            DynValue function = script.DoString(
                @"
                return function()
                    local function inner()
                        coroutine.yield('pause')
                    end

                    inner()
                end
            "
            );

            DynValue coroutine = script.CreateCoroutine(function);
            DynValue yielded = coroutine.Coroutine.Resume();
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
    }
}
