namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
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
        public void LoadPropagatesDecoratedMessageWhenReaderThrowsSyntaxError()
        {
            Script script = new(CoreModules.PresetComplete);
            script.Globals["throw_reader_helper"] = DynValue.NewCallback(
                (_, _) => throw new SyntaxErrorException("reader failure")
            );

            DynValue loadResult = script.DoString(
                @"
                local function throwing_reader()
                    return throw_reader_helper()
                end
                return load(throwing_reader)
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(loadResult.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(loadResult.Tuple[0].IsNil());
                Assert.That(loadResult.Tuple[1].String, Does.Contain("reader failure"));
                Assert.That(loadResult.Tuple[1].String, Does.Contain("chunk_"));
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
        public void LoadCompilesStringChunksAndUsesProvidedSourceName()
        {
            Script script = new(CoreModules.PresetComplete);
            Table env = new(script);
            env["value"] = DynValue.NewNumber(321);

            DynValue chunk = script.LoadString("return value", env, "chunk-string");
            DynValue result = script.Call(chunk);

            Assert.That(result.Number, Is.EqualTo(321d));

            DynValue failingChunk = script.LoadString(
                "error('boom')",
                new Table(script),
                "chunk-string"
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(failingChunk)
            )!;

            string message = exception.DecoratedMessage ?? exception.Message;
            Assert.That(message, Does.Contain("chunk-string"));
        }

        [Test]
        public void LoadRejectsChunkSourcesThatAreNeitherStringNorFunction()
        {
            Script script = new(CoreModules.PresetComplete);

            Assert.That(
                () => script.DoString("load(true)"),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.Contain("function expected")
            );
        }

        [Test]
        public void LoadReturnsTupleWithSyntaxErrorWhenStringIsInvalid()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue result = script.DoString("return load('function(')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].IsNil());
                Assert.That(result.Tuple[1].String, Does.Contain("unexpected symbol near '('"));
            });
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
        public void LoadFileHonorsExplicitEnvironmentParameter()
        {
            Script script = new(CoreModules.PresetComplete);
            RecordingScriptLoader loader = new() { ModuleBody = "return value" };
            script.Options.ScriptLoader = loader;
            script.Globals["value"] = DynValue.NewString("global");

            DynValue result = script.DoString(
                @"
                local env = { value = 'from-env' }
                local fn = loadfile('module.lua', 't', env)
                return fn()
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.String, Is.EqualTo("from-env"));
                Assert.That(loader.LoadCount, Is.EqualTo(1));
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
        public void LoadFileUsesRawMessageWhenScriptLoaderThrowsSyntaxErrorWithoutDecoration()
        {
            Script script = new(CoreModules.PresetComplete);
            script.Options.ScriptLoader = new ThrowingSyntaxErrorScriptLoader();

            DynValue result = script.DoString("return loadfile('anything.lua')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].IsNil());
                Assert.That(result.Tuple[1].String, Is.EqualTo("loader failure"));
            });
        }

        [Test]
        public void GetSyntaxErrorMessagePrefersDecoratedTextWhenAvailable()
        {
            SyntaxErrorException exception = new("raw message")
            {
                DecoratedMessage = "decorated message",
            };

            string message = LoadModule.GetSyntaxErrorMessage(exception);

            Assert.That(message, Is.EqualTo("decorated message"));
        }

        [Test]
        public void GetSyntaxErrorMessageFallsBackToRawMessageWhenDecorationMissing()
        {
            SyntaxErrorException exception = new("raw message") { DecoratedMessage = null };

            string message = LoadModule.GetSyntaxErrorMessage(exception);

            Assert.That(message, Is.EqualTo("raw message"));
        }

        [Test]
        public void GetSyntaxErrorMessageReturnsEmptyStringWhenExceptionIsNull()
        {
            Assert.That(LoadModule.GetSyntaxErrorMessage(null), Is.EqualTo(string.Empty));
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

        [Test]
        public void NovaSharpInitCreatesPackageTableWhenMissing()
        {
            Script script = new(CoreModules.PresetComplete);
            Table globals = new(script);
            Table ioTable = new(script);

            LoadModule.NovaSharpInit(globals, ioTable);

            DynValue package = globals.Get("package");
            Assert.Multiple(() =>
            {
                Assert.That(package.Type, Is.EqualTo(DataType.Table));
                Assert.That(
                    package.Table.Get("config").String,
                    Is.EqualTo($"{Path.DirectorySeparatorChar}\n;\n?\n!\n-\n")
                );
            });
        }

        [Test]
        public void NovaSharpInitThrowsWhenPackageIsNotTable()
        {
            Script script = new(CoreModules.PresetComplete);
            Table globals = new(script);
            globals["package"] = DynValue.NewNumber(42);

            Assert.That(
                () => LoadModule.NovaSharpInit(globals, new Table(script)),
                Throws
                    .TypeOf<InternalErrorException>()
                    .With.Message.Contain(
                        "'package' global variable was found and it is not a table"
                    )
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

        private sealed class ThrowingSyntaxErrorScriptLoader : IScriptLoader
        {
            public object LoadFile(string file, Table globalContext)
            {
                throw new SyntaxErrorException("loader failure");
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
