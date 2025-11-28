namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Modules;

    public sealed class ScriptCallTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CallWithNullDynValueArgsThrows()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function noop() end");
            DynValue function = script.Globals.Get("noop");

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                script.Call(function, (DynValue[])null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        public async Task CallWithNullObjectArgsThrows()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function noop() end");
            DynValue function = script.Globals.Get("noop");

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                script.Call(function, (object[])null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        public async Task CallWithNullFunctionThrows()
        {
            Script script = new(CoreModules.PresetComplete);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                script.Call((DynValue)null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("function");
        }

        [global::TUnit.Core.Test]
        public async Task CallInvokesMetamethodWhenValueHasCall()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                local mt = {}
                function mt:__call(value)
                    return value * 2
                end
                callable = setmetatable({}, mt)
            "
            );

            DynValue callable = script.Globals.Get("callable");
            DynValue result = script.Call(callable, DynValue.NewNumber(21));

            await Assert.That(result.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task CallExecutesClrFunction()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.NewString("clr"));

            DynValue result = script.Call(callback);

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("clr");
        }

        [global::TUnit.Core.Test]
        public async Task CallRejectsNonCallableValues()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue notCallable = DynValue.NewString("nope");

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                script.Call(notCallable)
            );

            await Assert.That(exception.Message).Contains("has no __call metamethod");
        }

        [global::TUnit.Core.Test]
        public async Task CallWithObjectArgumentsConvertsValues()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function add(a, b) return a + b end");
            DynValue function = script.Globals.Get("add");

            DynValue result = script.Call(function, 30, 12);
            await Assert.That(result.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task CallObjectOverloadInvokesClosureAndConvertsArguments()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function mul(a, b) return a * b end");
            object closure = script.Globals.Get("mul").Function;

            DynValue result = script.Call(closure, 6, 7);
            await Assert.That(result.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task CallObjectOverloadInvokesDelegateCallback()
        {
            Script script = new(CoreModules.PresetComplete);
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = (ctx, args) =>
                DynValue.NewNumber(args[0].Number * 2);

            DynValue result = script.Call(callback, 21);
            await Assert.That(result.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task CallObjectOverloadRejectsNonCallableValues()
        {
            Script script = new(CoreModules.PresetComplete);

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                script.Call((object)"not callable")
            );
            await Assert.That(exception.Message).Contains("__call metamethod");
        }

        [global::TUnit.Core.Test]
        public async Task CallObjectOverloadThrowsWhenFunctionNull()
        {
            Script script = new(CoreModules.PresetComplete);

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                script.Call((object)null)
            );
            await Assert.That(exception.Message).Contains("__call metamethod");
        }

        [global::TUnit.Core.Test]
        public async Task CreateCoroutineValidatesInputs()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.NewString("done"));

            DynValue coroutine = script.CreateCoroutine(callback);
            await Assert.That(coroutine.Type).IsEqualTo(DataType.Thread);

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                script.CreateCoroutine(DynValue.NewNumber(1))
            );
            await Assert.That(exception.Message).Contains("DataType.Function");
        }

        [global::TUnit.Core.Test]
        public async Task CreateCoroutineThrowsWhenFunctionNull()
        {
            Script script = new(CoreModules.PresetComplete);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                script.CreateCoroutine((DynValue)null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("function");
        }

        [global::TUnit.Core.Test]
        public async Task RequireModuleWarnsWhenBit32NotSupported()
        {
            StubScriptLoader loader = new()
            {
                ModuleSource = "return function() return 'bit32' end",
            };
            List<string> messages = new();
            ScriptOptions options = new() { ScriptLoader = loader, DebugPrint = messages.Add };
            Script script = new(CoreModules.PresetComplete, options);

            DynValue result = script.RequireModule("bit32");

            await Assert.That(loader.ResolveCalls).IsEqualTo(1);
            await Assert.That(loader.LoadCalls).IsEqualTo(1);
            await Assert
                .That(
                    messages.Exists(static value =>
                        value.Contains("require('bit32')", StringComparison.Ordinal)
                    )
                )
                .IsTrue();
            await Assert.That(result.Type).IsEqualTo(DataType.Function);
        }

        [global::TUnit.Core.Test]
        public async Task RequireModuleThrowsWhenModuleMissing()
        {
            StubScriptLoader loader = new() { ResolveReturnsNull = true };
            ScriptOptions options = new() { ScriptLoader = loader };
            Script script = new(CoreModules.PresetComplete, options);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.RequireModule("missing")
            );
            await Assert.That(exception.Message).Contains("module 'missing' not found");
        }

        [global::TUnit.Core.Test]
        public async Task RequireModuleWarnsOnlyOnceForBit32()
        {
            StubScriptLoader loader = new() { ModuleSource = "return function() end" };
            List<string> messages = new();
            ScriptOptions options = new() { ScriptLoader = loader, DebugPrint = messages.Add };
            Script script = new(CoreModules.PresetComplete, options);

            script.RequireModule("bit32");
            script.RequireModule("bit32");

            await Assert.That(messages.Count).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task RequireModuleDoesNotWarnWhenProfileSupportsBit32()
        {
            StubScriptLoader loader = new() { ModuleSource = "return function() end" };
            List<string> messages = new();
            ScriptOptions options = new()
            {
                ScriptLoader = loader,
                DebugPrint = messages.Add,
                CompatibilityVersion = LuaCompatibilityVersion.Lua52,
            };
            Script script = new(CoreModules.PresetComplete, options);

            script.RequireModule("bit32");
            await Assert.That(messages.Count).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task RequireModuleUsesProvidedGlobalContext()
        {
            StubScriptLoader loader = new();
            Script script = new(
                CoreModules.PresetComplete,
                new ScriptOptions { ScriptLoader = loader }
            );
            Table customGlobals = new(script);

            script.RequireModule("custom", customGlobals);

            await Assert.That(loader.ResolveCalls).IsEqualTo(1);
            await Assert.That(loader.LastGlobalContext).IsSameReferenceAs(customGlobals);
        }

        [global::TUnit.Core.Test]
        public async Task RequireModuleDefaultsToScriptGlobals()
        {
            StubScriptLoader loader = new();
            Script script = new(
                CoreModules.PresetComplete,
                new ScriptOptions { ScriptLoader = loader }
            );

            script.RequireModule("custom");

            await Assert.That(loader.LastGlobalContext).IsSameReferenceAs(script.Globals);
        }

        [global::TUnit.Core.Test]
        public async Task RequireModuleThrowsWhenGlobalContextOwnedByDifferentScript()
        {
            StubScriptLoader loader = new();
            Script script = new(
                CoreModules.PresetComplete,
                new ScriptOptions { ScriptLoader = loader }
            );
            Script foreignScript = new(CoreModules.PresetComplete);
            Table foreignGlobals = new(foreignScript);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.RequireModule("custom", foreignGlobals)
            );

            await Assert.That(exception.Message).Contains("different scripts");
        }

        [global::TUnit.Core.Test]
        public async Task CallRejectsValuesOwnedByDifferentScripts()
        {
            Script scriptA = new(CoreModules.PresetComplete);
            Script scriptB = new(CoreModules.PresetComplete);

            DynValue foreignTable = scriptA.DoString("return {}");
            scriptB.DoString("function echo(value) return value end");

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                scriptB.Call(scriptB.Globals.Get("echo"), foreignTable)
            );

            await Assert.That(exception.Message).Contains("different scripts");
        }

        [global::TUnit.Core.Test]
        public async Task CallObjectOverloadRejectsForeignClosure()
        {
            Script scriptA = new(CoreModules.PresetComplete);
            scriptA.DoString("function noop() return 1 end");
            object foreignClosure = scriptA.Globals.Get("noop").Function;

            Script scriptB = new(CoreModules.PresetComplete);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                scriptB.Call(foreignClosure)
            );

            await Assert.That(exception.Message).Contains("different scripts");
        }

        [global::TUnit.Core.Test]
        public async Task CreateCoroutineRejectsFunctionsOwnedByDifferentScripts()
        {
            Script scriptA = new(CoreModules.PresetComplete);
            Script scriptB = new(CoreModules.PresetComplete);
            DynValue foreignFunction = scriptA.DoString("return function() end");

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                scriptB.CreateCoroutine(foreignFunction)
            );

            await Assert.That(exception.Message).Contains("different scripts");
        }

        [global::TUnit.Core.Test]
        public async Task CreateCoroutineObjectOverloadUsesClosure()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function generator()
                    coroutine.yield(5)
                    return 6
                end
            "
            );

            object closure = script.Globals.Get("generator").Function;
            DynValue coroutine = script.CreateCoroutine(closure);

            DynValue first = coroutine.Coroutine.Resume();
            DynValue second = coroutine.Coroutine.Resume();

            await Assert.That(first.Number).IsEqualTo(5);
            await Assert.That(second.Number).IsEqualTo(6);
        }

        [global::TUnit.Core.Test]
        public async Task CreateCoroutineObjectOverloadSupportsDelegates()
        {
            Script script = new(CoreModules.PresetComplete);
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = (ctx, _) =>
                DynValue.NewNumber(99);

            DynValue coroutineValue = script.CreateCoroutine(callback);
            coroutineValue.Coroutine.OwnerScript = script;
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            DynValue result = coroutineValue.Coroutine.Resume(context);

            await Assert.That(result.Number).IsEqualTo(99);
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task CreateCoroutineObjectOverloadRejectsNonCallable()
        {
            Script script = new(CoreModules.PresetComplete);

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                script.CreateCoroutine((object)"invalid")
            );
            await Assert.That(exception.Message).Contains("DataType.Function");
        }

        [global::TUnit.Core.Test]
        public async Task CreateCoroutineObjectOverloadRejectsForeignClosure()
        {
            Script scriptA = new(CoreModules.PresetComplete);
            scriptA.DoString("function noop() return 0 end");
            object foreignClosure = scriptA.Globals.Get("noop").Function;

            Script scriptB = new(CoreModules.PresetComplete);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                scriptB.CreateCoroutine(foreignClosure)
            );

            await Assert.That(exception.Message).Contains("different scripts");
        }

        private sealed class StubScriptLoader : ScriptLoaderBase
        {
            internal int ResolveCalls { get; private set; }
            internal int LoadCalls { get; private set; }
            internal bool ResolveReturnsNull { get; set; }
            internal string ModuleSource { get; set; } = "return function() end";
            internal Table LastGlobalContext { get; private set; }

            public override object LoadFile(string file, Table globalContext)
            {
                LoadCalls++;
                return ModuleSource;
            }

            public override bool ScriptFileExists(string name)
            {
                return true;
            }

            public override string ResolveModuleName(string modname, Table globalContext)
            {
                LastGlobalContext = globalContext;
                ResolveCalls++;
                return ResolveReturnsNull ? null : modname;
            }
        }

        private static TException ExpectException<TException>(Func<DynValue> action)
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
