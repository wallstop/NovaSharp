namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptCallTests
    {
        [Test]
        public void CallWithNullDynValueArgsThrows()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function noop() end");
            DynValue function = script.Globals.Get("noop");

            Assert.That(
                () => script.Call(function, (DynValue[])null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("args")
            );
        }

        [Test]
        public void CallWithNullObjectArgsThrows()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function noop() end");
            DynValue function = script.Globals.Get("noop");

            Assert.That(
                () => script.Call(function, (object[])null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("args")
            );
        }

        [Test]
        public void CallInvokesMetamethodWhenValueHasCall()
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

            Assert.That(result.Number, Is.EqualTo(42));
        }

        [Test]
        public void CallExecutesClrFunction()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.NewString("clr"));

            DynValue result = script.Call(callback);

            Assert.That(result.Type, Is.EqualTo(DataType.String));
            Assert.That(result.String, Is.EqualTo("clr"));
        }

        [Test]
        public void CallRejectsNonCallableValues()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue notCallable = DynValue.NewString("nope");

            Assert.That(
                () => script.Call(notCallable),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("has no __call metamethod")
            );
        }

        [Test]
        public void CallWithObjectArgumentsConvertsValues()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function add(a, b) return a + b end");
            DynValue function = script.Globals.Get("add");

            DynValue result = script.Call(function, 30, 12);

            Assert.That(result.Number, Is.EqualTo(42));
        }

        [Test]
        public void CreateCoroutineValidatesInputs()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.NewString("done"));

            DynValue coroutine = script.CreateCoroutine(callback);
            Assert.That(coroutine.Type, Is.EqualTo(DataType.Thread));

            Assert.That(
                () => script.CreateCoroutine(DynValue.NewNumber(1)),
                Throws.ArgumentException.With.Message.Contains("DataType.Function")
            );
        }

        [Test]
        public void RequireModuleWarnsWhenBit32NotSupported()
        {
            StubScriptLoader loader = new()
            {
                ModuleSource = "return function() return 'bit32' end",
            };
            List<string> messages = new();
            ScriptOptions options = new() { ScriptLoader = loader, DebugPrint = messages.Add };
            Script script = new(CoreModules.PresetComplete, options);

            DynValue result = script.RequireModule("bit32");

            Assert.Multiple(() =>
            {
                Assert.That(loader.ResolveCalls, Is.EqualTo(1));
                Assert.That(loader.LoadCalls, Is.EqualTo(1));
                Assert.That(messages, Has.Some.Contains("require('bit32')"));
                Assert.That(result.Type, Is.EqualTo(DataType.Function));
            });
        }

        [Test]
        public void RequireModuleThrowsWhenModuleMissing()
        {
            StubScriptLoader loader = new() { ResolveReturnsNull = true };
            ScriptOptions options = new() { ScriptLoader = loader };
            Script script = new(CoreModules.PresetComplete, options);

            Assert.That(
                () => script.RequireModule("missing"),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("module 'missing' not found")
            );
        }

        [Test]
        public void RequireModuleWarnsOnlyOnceForBit32()
        {
            StubScriptLoader loader = new() { ModuleSource = "return function() end" };
            List<string> messages = new();
            ScriptOptions options = new() { ScriptLoader = loader, DebugPrint = messages.Add };
            Script script = new(CoreModules.PresetComplete, options);

            script.RequireModule("bit32");
            script.RequireModule("bit32");

            Assert.That(messages.Count, Is.EqualTo(1));
        }

        [Test]
        public void RequireModuleDoesNotWarnWhenProfileSupportsBit32()
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

            Assert.That(messages, Is.Empty);
        }

        [Test]
        public void RequireModuleUsesProvidedGlobalContext()
        {
            StubScriptLoader loader = new();
            Script script = new(
                CoreModules.PresetComplete,
                new ScriptOptions { ScriptLoader = loader }
            );
            Table customGlobals = new(script);

            script.RequireModule("custom", customGlobals);

            Assert.Multiple(() =>
            {
                Assert.That(loader.ResolveCalls, Is.EqualTo(1));
                Assert.That(loader.LastGlobalContext, Is.SameAs(customGlobals));
            });
        }

        [Test]
        public void RequireModuleDefaultsToScriptGlobals()
        {
            StubScriptLoader loader = new();
            Script script = new(
                CoreModules.PresetComplete,
                new ScriptOptions { ScriptLoader = loader }
            );

            script.RequireModule("custom");

            Assert.That(loader.LastGlobalContext, Is.SameAs(script.Globals));
        }

        [Test]
        public void RequireModuleThrowsWhenGlobalContextOwnedByDifferentScript()
        {
            StubScriptLoader loader = new();
            Script script = new(
                CoreModules.PresetComplete,
                new ScriptOptions { ScriptLoader = loader }
            );
            Script foreignScript = new(CoreModules.PresetComplete);
            Table foreignGlobals = new(foreignScript);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.RequireModule("custom", foreignGlobals)
            )!;

            Assert.That(exception.Message, Does.Contain("different scripts"));
        }

        [Test]
        public void CallRejectsValuesOwnedByDifferentScripts()
        {
            Script scriptA = new(CoreModules.PresetComplete);
            Script scriptB = new(CoreModules.PresetComplete);

            DynValue foreignTable = scriptA.DoString("return {}");
            scriptB.DoString("function echo(value) return value end");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.Call(scriptB.Globals.Get("echo"), foreignTable)
            )!;

            Assert.That(exception.Message, Does.Contain("different scripts"));
        }

        [Test]
        public void CreateCoroutineRejectsFunctionsOwnedByDifferentScripts()
        {
            Script scriptA = new(CoreModules.PresetComplete);
            Script scriptB = new(CoreModules.PresetComplete);
            DynValue foreignFunction = scriptA.DoString("return function() end");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.CreateCoroutine(foreignFunction)
            )!;

            Assert.That(exception.Message, Does.Contain("different scripts"));
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
    }
}
