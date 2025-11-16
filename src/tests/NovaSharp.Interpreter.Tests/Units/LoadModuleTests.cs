namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LoadModuleTests
    {
        [Test]
        public void RequireCachesModuleResult()
        {
            Script script = new(CoreModules.PresetComplete);
            RecordingScriptLoader loader = new()
            {
                ModuleBody = "local name = ...; return { value = name, timestamp = os.time() }",
            };
            script.Options.ScriptLoader = loader;

            DynValue requireFunc = script.Globals.Get("require");
            Assert.That(
                requireFunc.Type,
                Is.EqualTo(DataType.Function),
                "require must be available"
            );

            DynValue first = script.Call(requireFunc, DynValue.NewString("modules.sample"));
            DynValue second = script.Call(requireFunc, DynValue.NewString("modules.sample"));

            Assert.Multiple(() =>
            {
                Assert.That(first.Type, Is.EqualTo(DataType.Table));
                Assert.That(first.Table.Get("value").String, Is.EqualTo("modules.sample"));
                Assert.That(loader.LoadCount, Is.EqualTo(1), "module body is loaded once");
                Assert.That(second.Table, Is.SameAs(first.Table), "cached module is returned");
            });
        }

        [Test]
        public void RequireThrowsWhenModuleCannotBeResolved()
        {
            Script script = new(CoreModules.PresetComplete);
            script.Options.ScriptLoader = new NullResolvingScriptLoader();

            Assert.That(
                () => script.DoString("return require('missing.module')"),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contain("module 'missing.module' not found")
            );
        }

        [Test]
        public void LoadReturnsTupleWithErrorWhenReaderYieldsNonString()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue loadResult = script.DoString(
                @"
                local called = false
                local function badreader()
                    if called then
                        return nil
                    end
                    called = true
                    return {}
                end

                return load(badreader)
            "
            );

            Assert.Multiple(() =>
            {
                Assert.That(loadResult.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(loadResult.Tuple[0].IsNil());
                Assert.That(
                    loadResult.Tuple[1].String,
                    Does.Contain("reader function must return a string")
                );
            });
        }

        [Test]
        public void LoadConcatenatesReaderFragmentsAndUsesProvidedEnvironment()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue result = script.DoString(
                @"
                local fragments = { 'return ', 'value', nil }
                local index = 0
                local reader = function()
                    index = index + 1
                    return fragments[index]
                end
                local env = { value = 123 }
                local chunk, err = load(reader, 'chunk-fragments', 't', env)
                assert(chunk ~= nil and err == nil)
                return chunk()
                "
            );

            Assert.That(result.Number, Is.EqualTo(123d));
        }

        [Test]
        public void LoadFileSafeUsesSafeEnvironmentWhenNotProvided()
        {
            Script script = new(CoreModules.PresetComplete);
            RecordingScriptLoader loader = new() { ModuleBody = "return marker" };
            script.Options.ScriptLoader = loader;
            script.Globals["marker"] = DynValue.NewString("global");

            DynValue executionResult = script.DoString(
                "local fn = loadfilesafe('safe.lua'); return fn()"
            );

            Assert.Multiple(() =>
            {
                Assert.That(executionResult.String, Is.EqualTo("global"));
                Assert.That(loader.LoadCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void LoadSafeThrowsWhenEnvironmentCannotBeRetrieved()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue result = script.DoString(
                @"
                local original_env = _ENV
                local ls = loadsafe
                local pc = pcall
                _ENV = nil
                local ok, err = pc(function() return ls('return 1') end)
                _ENV = original_env
                return ok, err
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Boolean, Is.False);
                Assert.That(
                    result.Tuple[1].String,
                    Does.Contain("current environment cannot be backtracked")
                );
            });
        }

        [Test]
        public void LoadFileReturnsTupleWithSyntaxErrorMessage()
        {
            Script script = new(CoreModules.PresetComplete);
            script.Options.ScriptLoader = new SyntaxErrorScriptLoader();

            DynValue loadFileResult = script.DoString("return loadfile('broken.lua')");

            Assert.Multiple(() =>
            {
                Assert.That(loadFileResult.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(loadFileResult.Tuple[0].IsNil());
                Assert.That(
                    loadFileResult.Tuple[1].String,
                    Does.Contain("unexpected symbol near '('")
                );
            });
        }

        [Test]
        public void DoFileExecutesLoadedChunk()
        {
            Script script = new(CoreModules.PresetComplete);
            RecordingScriptLoader loader = new() { ModuleBody = "return 777" };
            script.Options.ScriptLoader = loader;

            DynValue value = script.DoString("return dofile('script.lua')");

            Assert.Multiple(() =>
            {
                Assert.That(value.Number, Is.EqualTo(777d));
                Assert.That(loader.LoadCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void DoFileWrapsSyntaxErrorsWithScriptRuntimeException()
        {
            Script script = new(CoreModules.PresetComplete);
            script.Options.ScriptLoader = new SyntaxErrorScriptLoader();

            Assert.That(
                () => script.DoString("dofile('broken.lua')"),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contain("unexpected symbol near '('")
            );
        }

        private sealed class RecordingScriptLoader : IScriptLoader
        {
            public string ModuleBody { get; set; } =
                "return function(name) return { value = name } end";

            public int LoadCount { get; private set; }

            public object LoadFile(string file, Table globalContext)
            {
                LoadCount++;
                return ModuleBody;
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return modname;
            }
        }

        private sealed class NullResolvingScriptLoader : IScriptLoader
        {
            public object LoadFile(string file, Table globalContext)
            {
                throw new InvalidOperationException("LoadFile should not be invoked.");
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return null!;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return null!;
            }
        }

        private sealed class SyntaxErrorScriptLoader : IScriptLoader
        {
            public object LoadFile(string file, Table globalContext)
            {
                return "function(";
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return modname;
            }
        }
    }
}
