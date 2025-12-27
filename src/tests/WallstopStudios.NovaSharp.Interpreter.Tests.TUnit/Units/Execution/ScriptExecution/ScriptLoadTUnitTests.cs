namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Tests.TestUtilities;
    using WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class ScriptLoadTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringDecodesBase64Dump(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue chunk = script.LoadString("return 77");

            string encodedDump = EncodeFunctionAsBase64(script, chunk);
            DynValue loaded = script.LoadString(encodedDump);
            DynValue result = script.Call(loaded);

            await Assert.That(result.Number).IsEqualTo(77);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStreamExecutesTextWhenStreamIsNotDump(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            using MemoryStream stream = new(Encoding.UTF8.GetBytes("return 13"));

            DynValue function = script.LoadStream(stream);
            DynValue result = script.Call(function);

            await Assert.That(result.Number).IsEqualTo(13);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStreamHandlesShortTextStreams(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            using MemoryStream stream = new(Encoding.UTF8.GetBytes("x=41"));

            DynValue chunk = script.LoadStream(stream);
            script.Call(chunk);

            await Assert.That(script.Globals.Get("x").Number).IsEqualTo(41);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStreamUndumpsBinaryChunk(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            byte[] dump = DumpToBytes(script, script.LoadString("return 19"));
            using MemoryStream stream = new(dump, writable: false);

            DynValue function = script.LoadStream(stream);
            DynValue result = script.Call(function);

            await Assert.That(result.Number).IsEqualTo(19);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFunctionBindsProvidedEnvironment(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table env = new(script);
            env.Set("value", DynValue.NewNumber(7));

            DynValue closure = script.LoadFunction("function() return value end", env, "bound");
            DynValue result = script.Call(closure);

            await Assert.That(result.Number).IsEqualTo(7);
            await Assert.That(script.Globals.Get("value").IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStreamUndumpPreservesEnvironmentUpValues(
            LuaCompatibilityVersion version
        )
        {
            Script producer = new();
            producer.Globals.Set("shared", DynValue.NewNumber(5));
            DynValue chunk = producer.LoadString(
                @"
                return function()
                    return shared
                end
            "
            );
            byte[] dump = DumpToBytes(producer, chunk);

            Script consumer = new();
            consumer.Globals.Set("shared", DynValue.NewNumber(10));

            using MemoryStream stream = new(dump, writable: false);
            DynValue chunkResult = consumer.LoadStream(stream);
            DynValue restoredFunction = consumer.Call(chunkResult);
            DynValue result = consumer.Call(restoredFunction);

            await Assert.That(result.Number).IsEqualTo(10);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFileInvokesLegacyResolveFileNameFallback(
            LuaCompatibilityVersion version
        )
        {
            LegacyScriptLoader loader = new();
            Script script = new(new ScriptOptions { ScriptLoader = loader });

            DynValue chunk = script.LoadFile("   legacy.lua  ");
            DynValue result = script.Call(chunk);

            await Assert.That(loader.WasResolveFileNameCalled).IsTrue();
            await Assert.That(loader.LastResolvedFilename).IsEqualTo("   legacy.lua  ");
            await Assert.That(loader.LastLoadedFile).IsEqualTo("legacy.lua");
            await Assert.That(result.Number).IsEqualTo(77);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFileExecutesStringsBytesAndStreamsFromLoader(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            StubScriptLoader loader = new() { Mode = LoaderMode.String, Source = "return 21" };
            script.Options.ScriptLoader = loader;

            DynValue fromString = script.LoadFile("string.lua");
            await Assert.That(script.Call(fromString).Number).IsEqualTo(21);

            loader.Mode = LoaderMode.Bytes;
            loader.Source = "return 22";
            DynValue fromBytes = script.LoadFile("bytes.lua");
            await Assert.That(script.Call(fromBytes).Number).IsEqualTo(22);

            loader.Mode = LoaderMode.Stream;
            loader.Source = "return 23";
            DynValue fromStream = script.LoadFile("stream.lua");
            await Assert.That(script.Call(fromStream).Number).IsEqualTo(23);
            await Assert.That(loader.StreamDisposed).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RequireModuleLoadsChunkViaScriptLoader(LuaCompatibilityVersion version)
        {
            ModuleScriptLoader loader = new() { ModuleCode = "return 42" };
            Script script = new(new ScriptOptions { ScriptLoader = loader });

            DynValue module = script.RequireModule("answer");
            DynValue result = script.Call(module);

            await Assert.That(loader.ResolvedModuleNames).IsEquivalentTo(AnswerModuleName);
            await Assert.That(loader.LoadedFiles).IsEquivalentTo(AnswerModuleFile);
            await Assert.That(result.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateDynamicExpressionRegistersSourceAndEvaluates(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            int initialCount = script.SourceCodeCount;

            DynamicExpression expression = script.CreateDynamicExpression("value * 2");

            await Assert.That(script.SourceCodeCount).IsEqualTo(initialCount + 1);
            await Assert.That(script.GetSourceCode(initialCount).Name).Contains("__dynamic_");

            script.Globals.Set("value", DynValue.NewNumber(8));
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            await Assert.That(expression.Evaluate(context).Number).IsEqualTo(16);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStreamThrowsWhenStreamNull(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                script.LoadStream(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("stream");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadStringThrowsWhenCodeNull(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                script.LoadString(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("code");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFileThrowsWhenFilenameNull(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                script.LoadFile(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("filename");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadFileThrowsOnNullAndUnsupportedResults(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            StubScriptLoader loader = new() { Mode = LoaderMode.Null };
            script.Options.ScriptLoader = loader;

            InvalidCastException nullException = ExpectException<InvalidCastException>(() =>
                script.LoadFile("broken.lua")
            );
            await Assert.That(nullException.Message).Contains("Unexpected null");

            loader.Mode = LoaderMode.Unsupported;
            InvalidCastException unsupportedException = ExpectException<InvalidCastException>(() =>
                script.LoadFile("still-broken.lua")
            );
            await Assert.That(unsupportedException.Message).Contains("Unsupported return type");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ScriptUsesCustomTimeProviderForStartTimestamp(
            LuaCompatibilityVersion version
        )
        {
            DateTimeOffset timestamp = DateTimeOffset.Parse(
                "2025-11-16T10:00:00Z",
                CultureInfo.InvariantCulture
            );
            FrozenTimeProvider provider = new(timestamp);
            ScriptOptions options = new(Script.DefaultOptions) { TimeProvider = provider };
            Script script = new(options);

            await Assert.That(ReferenceEquals(script.TimeProvider, provider)).IsTrue();
            await Assert.That(script.StartTimeUtc).IsEqualTo(timestamp.UtcDateTime);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallInvokesMetamethodWhenValueIsCallable(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue callableTable = script.DoString(
                @"
                local t = {}
                setmetatable(t, { __call = function(_, value) return value * 2 end })
                return t
                "
            );

            DynValue result = script.Call(callableTable, DynValue.NewNumber(6));
            await Assert.That(result.Number).IsEqualTo(12);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallExecutesClrFunctionCallbacks(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue callback = DynValue.NewCallback(
                (_, args) => DynValue.NewNumber(args[0].Number + 5)
            );

            DynValue result = script.Call(callback, DynValue.NewNumber(7));
            await Assert.That(result.Number).IsEqualTo(12);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallConvertsObjectArgumentsToDynValues(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue chunk = script.LoadString("return function(value) return value * 3 end");
            DynValue multiplier = script.Call(chunk);

            DynValue result = script.Call(multiplier, 4);
            await Assert.That(result.Number).IsEqualTo(12);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CallComplainsWhenValueIsNotCallable(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                script.Call(DynValue.NewString("nope"))
            );

            await Assert.That(exception.Message).Contains("has no __call metamethod");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateCoroutineSupportsClrFunctionsAndValidatesInputs(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.NewString("done"));

            DynValue coroutine = script.CreateCoroutine(callback);
            await Assert.That(coroutine.Type).IsEqualTo(DataType.Thread);

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                script.CreateCoroutine(DynValue.NewNumber(1))
            );
            await Assert.That(exception.Message).Contains("function");
        }

        [global::TUnit.Core.Test]
        [PlatformDetectorIsolation]
        [ScriptDefaultOptionsIsolation]
        public async Task RunStringAndRunFileExecuteConvenienceHelpers()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();

            // Verify the script loader is correctly configured before running tests
            string loaderType = Script.DefaultOptions.ScriptLoader?.GetType().Name ?? "null";
            await Assert
                .That(loaderType)
                .IsEqualTo(nameof(FileSystemScriptLoader))
                .Because($"Expected FileSystemScriptLoader but got {loaderType}")
                .ConfigureAwait(false);

            DynValue stringResult = Script.RunString("return 321");
            await Assert
                .That(stringResult.Type)
                .IsEqualTo(DataType.Number)
                .Because($"RunString result type was {stringResult.Type}, expected Number")
                .ConfigureAwait(false);
            await Assert.That(stringResult.Number).IsEqualTo(321).ConfigureAwait(false);

            using TempFileScope tempFileScope = TempFileScope.Create(
                namePrefix: "script-",
                extension: ".lua"
            );
            string path = tempFileScope.FilePath;

            await File.WriteAllTextAsync(path, "return 654").ConfigureAwait(false);

            // Verify file was written correctly
            string fileContent = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            await Assert
                .That(fileContent)
                .IsEqualTo("return 654")
                .Because($"File content mismatch. Path: {path}, FileExists: {File.Exists(path)}")
                .ConfigureAwait(false);

            DynValue fileResult = Script.RunFile(path);

            // Provide diagnostic information on failure
            string diagnosticContext =
                $"Path: {path}, FileExists: {File.Exists(path)}, "
                + $"ScriptLoader: {Script.DefaultOptions.ScriptLoader?.GetType().Name ?? "null"}, "
                + $"ResultType: {fileResult.Type}, FileContent: '{fileContent}'";

            await Assert
                .That(fileResult.Type)
                .IsEqualTo(DataType.Number)
                .Because(diagnosticContext)
                .ConfigureAwait(false);
            await Assert
                .That(fileResult.Number)
                .IsEqualTo(654)
                .Because(diagnosticContext)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateDynamicExpressionRemovesSourceOnFailure(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            int initialCount = script.SourceCodeCount;

            SyntaxErrorException exception = ExpectException<SyntaxErrorException>(() =>
                script.CreateDynamicExpression("value +")
            );

            await Assert.That(exception.DecoratedMessage).Contains("unexpected symbol");
            await Assert.That(script.SourceCodeCount).IsEqualTo(initialCount);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RecycleCoroutineProducesFreshThread(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue source = CompileFunction(
                script,
                "function(value) coroutine.yield(value + 1); return value + 2 end"
            );
            Coroutine coroutine = script.CreateCoroutine(source).Coroutine;

            await Assert.That(coroutine.Resume(DynValue.NewNumber(3)).Number).IsEqualTo(4);
            await Assert.That(coroutine.Resume().Number).IsEqualTo(5);
            await Assert.That(coroutine.State).IsEqualTo(CoroutineState.Dead);

            DynValue replacement = CompileFunction(script, "function() return 99 end");
            DynValue recycled = script.RecycleCoroutine(coroutine, replacement);

            await Assert.That(recycled.Type).IsEqualTo(DataType.Thread);
            await Assert.That(recycled.Coroutine.State).IsEqualTo(CoroutineState.NotStarted);
            await Assert.That(recycled.Coroutine.Resume().Number).IsEqualTo(99);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RecycleCoroutineRequiresDeadCoroutineAndFunction(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue worker = CompileFunction(
                script,
                "function() coroutine.yield(1); return 2 end"
            );
            Coroutine live = script.CreateCoroutine(worker).Coroutine;

            ExpectException<InvalidOperationException>(() => script.RecycleCoroutine(live, worker));

            live.Resume();
            live.Resume();
            await Assert.That(live.State).IsEqualTo(CoroutineState.Dead);

            ExpectException<InvalidOperationException>(() =>
                script.RecycleCoroutine(live, DynValue.NewNumber(1))
            );
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public void RecycleCoroutineRequiresCoroutineInstance(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue worker = CompileFunction(script, "function() return 0 end");

            ExpectException<InvalidOperationException>(() => script.RecycleCoroutine(null, worker));
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DoStreamExecutesTextStreams(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            using MemoryStream stream = new(Encoding.UTF8.GetBytes("return 123"));

            DynValue result = script.DoStream(stream);
            await Assert.That(result.Number).IsEqualTo(123);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RequireModuleThrowsWhenLoaderCannotResolveName(
            LuaCompatibilityVersion version
        )
        {
            ModuleScriptLoader loader = new() { ModuleExists = false };
            Script script = new(new ScriptOptions { ScriptLoader = loader });

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.RequireModule("missing")
            );

            await Assert.That(exception.Message).Contains("module 'missing'");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DumpRejectsNonFunctionValues(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            using MemoryStream stream = new();

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                script.Dump(DynValue.NewNumber(1), stream)
            );

            await Assert.That(exception.Message).Contains("function arg is not a function");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DumpRejectsReadOnlyStreams(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue chunk = script.LoadString("return 1");
            using MemoryStream stream = new(Array.Empty<byte>(), writable: false);

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                script.Dump(chunk, stream)
            );

            await Assert.That(exception.Message).Contains("stream is readonly");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DumpRejectsFunctionsWithExternalUpValues(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue chunk = script.LoadString(
                @"
                local capture = 5
                return function() return capture end
                "
            );
            DynValue closure = script.Call(chunk);

            using MemoryStream stream = new();
            ArgumentException exception = ExpectException<ArgumentException>(() =>
                script.Dump(closure, stream)
            );

            await Assert.That(exception.Message).Contains("upvalues other than _ENV");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DumpThrowsWhenFunctionIsNull(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            using MemoryStream stream = new();

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                script.Dump(null, stream)
            );

            await Assert.That(exception.ParamName).IsEqualTo("function");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DumpThrowsWhenStreamIsNull(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue chunk = script.LoadString("return 1");

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                script.Dump(chunk, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("stream");
        }

        private static readonly string[] AnswerModuleName = { "answer" };
        private static readonly string[] AnswerModuleFile = { "answer.lua" };

        private static DynValue CompileFunction(Script script, string luaFunctionSource)
        {
            DynValue chunk = script.LoadString($"return {luaFunctionSource}");
            return script.Call(chunk);
        }

        private static string EncodeFunctionAsBase64(Script script, DynValue chunk)
        {
            using MemoryStream stream = new();
            script.Dump(chunk, stream);
            return StringModule.Base64DumpHeader + Convert.ToBase64String(stream.ToArray());
        }

        private static byte[] DumpToBytes(Script script, DynValue chunk)
        {
            using MemoryStream stream = new();
            script.Dump(chunk, stream);
            return stream.ToArray();
        }

        private enum LoaderMode
        {
            String,
            Bytes,
            Stream,
            Null,
            Unsupported,
        }

        private sealed class StubScriptLoader : IScriptLoader
        {
            public LoaderMode Mode { get; set; }

            public string Source { get; set; } = "return 0";

            public bool StreamDisposed { get; private set; }

            public object LoadFile(string file, Table globalContext)
            {
                return Mode switch
                {
                    LoaderMode.String => Source,
                    LoaderMode.Bytes => Encoding.UTF8.GetBytes(Source),
                    LoaderMode.Stream => new TrackingStream(
                        Encoding.UTF8.GetBytes(Source),
                        () => StreamDisposed = true
                    ),
                    LoaderMode.Null => null,
                    LoaderMode.Unsupported => new object(),
                    _ => throw new InvalidOperationException(),
                };
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return modname;
            }

            private sealed class TrackingStream : MemoryStream
            {
                private readonly Action _onDispose;

                public TrackingStream(byte[] buffer, Action onDispose)
                    : base(buffer, writable: false)
                {
                    _onDispose = onDispose;
                }

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        _onDispose();
                    }

                    base.Dispose(disposing);
                }
            }
        }

        private sealed class ModuleScriptLoader : IScriptLoader
        {
            private readonly List<string> _resolved = new();
            private readonly List<string> _loaded = new();

            public bool ModuleExists { get; set; } = true;

            public string ModuleCode { get; set; } = "return function() return 0 end";

            public IReadOnlyList<string> ResolvedModuleNames => _resolved;

            public IReadOnlyList<string> LoadedFiles => _loaded;

            public object LoadFile(string file, Table globalContext)
            {
                _loaded.Add(file);
                return ModuleCode;
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                _resolved.Add(modname);
                return ModuleExists ? $"{modname}.lua" : null;
            }
        }

        private sealed class LegacyScriptLoader : IScriptLoader
        {
            public bool WasResolveFileNameCalled { get; private set; }

            public string LastResolvedFilename { get; private set; }

            public string LastLoadedFile { get; private set; }

            public object LoadFile(string file, Table globalContext)
            {
                LastLoadedFile = file;
                return "return 77";
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                WasResolveFileNameCalled = true;
                LastResolvedFilename = filename;
                return filename?.Trim();
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return modname;
            }
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

    internal sealed class FrozenTimeProvider : ITimeProvider
    {
        private readonly DateTimeOffset _timestamp;

        public FrozenTimeProvider(DateTimeOffset timestamp)
        {
            _timestamp = timestamp;
        }

        public DateTimeOffset GetUtcNow()
        {
            return _timestamp;
        }
    }
}
