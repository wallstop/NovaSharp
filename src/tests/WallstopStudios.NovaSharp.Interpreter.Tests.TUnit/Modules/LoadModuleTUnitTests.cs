namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class LoadModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task RequireCachesModuleResult()
        {
            Script script = new(CoreModulePresets.Complete);
            RecordingScriptLoader loader = new()
            {
                ModuleBody = "local name = ...; return { value = name, timestamp = os.time() }",
            };
            script.Options.ScriptLoader = loader;

            DynValue requireFunc = script.Globals.Get("require");

            await Assert.That(requireFunc.Type).IsEqualTo(DataType.Function);

            DynValue first = script.Call(requireFunc, DynValue.NewString("modules.sample"));
            DynValue second = script.Call(requireFunc, DynValue.NewString("modules.sample"));

            await Assert.That(first.Type).IsEqualTo(DataType.Table);
            await Assert.That(first.Table.Get("value").String).IsEqualTo("modules.sample");
            await Assert.That(loader.LoadCount).IsEqualTo(1);
            await Assert.That(second.Table).IsSameReferenceAs(first.Table);
        }

        [global::TUnit.Core.Test]
        public async Task RequireThrowsWhenModuleCannotBeResolved()
        {
            Script script = new(CoreModulePresets.Complete);
            script.Options.ScriptLoader = new NullResolvingScriptLoader();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return require('missing.module')")
            );

            await Assert.That(exception.Message).Contains("module 'missing.module' not found");
        }

        [global::TUnit.Core.Test]
        public async Task LoadReturnsTupleWithErrorWhenReaderYieldsNonString()
        {
            Script script = new(CoreModulePresets.Complete);
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

            await Assert.That(loadResult.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(loadResult.Tuple[0].IsNil()).IsTrue();
            await Assert
                .That(loadResult.Tuple[1].String)
                .Contains("reader function must return a string");
        }

        [global::TUnit.Core.Test]
        public async Task LoadPropagatesDecoratedMessageWhenReaderThrowsSyntaxError()
        {
            Script script = new(CoreModulePresets.Complete);
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

            await Assert.That(loadResult.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(loadResult.Tuple[0].IsNil()).IsTrue();
            await Assert.That(loadResult.Tuple[1].String).Contains("reader failure");
            await Assert.That(loadResult.Tuple[1].String).Contains("chunk_");
        }

        [global::TUnit.Core.Test]
        public async Task LoadConcatenatesReaderFragmentsAndUsesProvidedEnvironment()
        {
            Script script = new(CoreModulePresets.Complete);
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

            await Assert.That(result.Number).IsEqualTo(123d);
        }

        [global::TUnit.Core.Test]
        public async Task LoadCompilesStringChunksAndUsesProvidedSourceName()
        {
            Script script = new(CoreModulePresets.Complete);
            Table env = new(script);
            env["value"] = DynValue.NewNumber(321);

            DynValue chunk = script.LoadString("return value", env, "chunk-string");
            DynValue result = script.Call(chunk);

            await Assert.That(result.Number).IsEqualTo(321d);

            DynValue failingChunk = script.LoadString(
                "error('boom')",
                new Table(script),
                "chunk-string"
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(failingChunk)
            );

            string message = exception.DecoratedMessage ?? exception.Message;
            await Assert.That(message).Contains("chunk-string");
        }

        [global::TUnit.Core.Test]
        public async Task LoadRejectsChunkSourcesThatAreNeitherStringNorFunction()
        {
            Script script = new(CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("load(true)")
            );

            await Assert.That(exception.Message).Contains("function expected");
        }

        [global::TUnit.Core.Test]
        public async Task LoadReturnsTupleWithSyntaxErrorWhenStringIsInvalid()
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue result = script.DoString("return load('function(')");

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("unexpected symbol near '('");
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileSafeUsesSafeEnvironmentWhenNotProvided()
        {
            Script script = new(CoreModulePresets.Complete);
            RecordingScriptLoader loader = new() { ModuleBody = "return marker" };
            script.Options.ScriptLoader = loader;
            script.Globals["marker"] = DynValue.NewString("global");

            DynValue executionResult = script.DoString(
                "local fn = loadfilesafe('safe.lua'); return fn()"
            );

            await Assert.That(executionResult.String).IsEqualTo("global");
            await Assert.That(loader.LoadCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task LoadSafeThrowsWhenEnvironmentCannotBeRetrieved()
        {
            Script script = new(CoreModulePresets.Complete);

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

            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert
                .That(result.Tuple[1].String)
                .Contains("current environment cannot be backtracked");
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileHonorsExplicitEnvironmentParameter()
        {
            Script script = new(CoreModulePresets.Complete);
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

            await Assert.That(result.String).IsEqualTo("from-env");
            await Assert.That(loader.LoadCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileReturnsTupleWithSyntaxErrorMessage()
        {
            Script script = new(CoreModulePresets.Complete);
            script.Options.ScriptLoader = new SyntaxErrorScriptLoader();

            DynValue loadFileResult = script.DoString("return loadfile('broken.lua')");

            await Assert.That(loadFileResult.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(loadFileResult.Tuple[0].IsNil()).IsTrue();
            await Assert
                .That(loadFileResult.Tuple[1].String)
                .Contains("unexpected symbol near '('");
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileUsesRawMessageWhenScriptLoaderThrowsSyntaxErrorWithoutDecoration()
        {
            Script script = new(CoreModulePresets.Complete);
            script.Options.ScriptLoader = new ThrowingSyntaxErrorScriptLoader();

            DynValue result = script.DoString("return loadfile('anything.lua')");

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("loader failure");
        }

        [global::TUnit.Core.Test]
        public async Task GetSyntaxErrorMessagePrefersDecoratedTextWhenAvailable()
        {
            SyntaxErrorException exception = new("raw message")
            {
                DecoratedMessage = "decorated message",
            };

            string message = LoadModule.GetSyntaxErrorMessage(exception);

            await Assert.That(message).IsEqualTo("decorated message");
        }

        [global::TUnit.Core.Test]
        public async Task GetSyntaxErrorMessageFallsBackToRawMessageWhenDecorationMissing()
        {
            SyntaxErrorException exception = new("raw message") { DecoratedMessage = null };

            string message = LoadModule.GetSyntaxErrorMessage(exception);

            await Assert.That(message).IsEqualTo("raw message");
        }

        [global::TUnit.Core.Test]
        public async Task GetSyntaxErrorMessageReturnsEmptyStringWhenExceptionIsNull()
        {
            await Assert.That(LoadModule.GetSyntaxErrorMessage(null)).IsEqualTo(string.Empty);
        }

        [global::TUnit.Core.Test]
        public async Task DoFileExecutesLoadedChunk()
        {
            Script script = new(CoreModulePresets.Complete);
            RecordingScriptLoader loader = new() { ModuleBody = "return 777" };
            script.Options.ScriptLoader = loader;

            DynValue value = script.DoString("return dofile('script.lua')");

            await Assert.That(value.Number).IsEqualTo(777d);
            await Assert.That(loader.LoadCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task DoFileWrapsSyntaxErrorsWithScriptRuntimeException()
        {
            Script script = new(CoreModulePresets.Complete);
            script.Options.ScriptLoader = new SyntaxErrorScriptLoader();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("dofile('broken.lua')")
            );

            await Assert.That(exception.Message).Contains("unexpected symbol near '('");
        }

        [global::TUnit.Core.Test]
        public async Task NovaSharpInitCreatesPackageTableWhenMissing()
        {
            Script script = new(CoreModulePresets.Complete);
            Table globals = new(script);
            Table ioTable = new(script);

            LoadModule.NovaSharpInit(globals, ioTable);

            DynValue package = globals.Get("package");
            await Assert.That(package.Type).IsEqualTo(DataType.Table);
            await Assert
                .That(package.Table.Get("config").String)
                .IsEqualTo($"{Path.DirectorySeparatorChar}\n;\n?\n!\n-\n");
        }

        [global::TUnit.Core.Test]
        public async Task NovaSharpInitThrowsWhenPackageIsNotTable()
        {
            Script script = new(CoreModulePresets.Complete);
            Table globals = new(script);
            globals["package"] = DynValue.NewNumber(42);

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                LoadModule.NovaSharpInit(globals, new Table(script))
            );

            await Assert
                .That(exception.Message)
                .Contains("'package' global variable was found and it is not a table");
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
                return null;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return null;
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
