namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    /// <summary>
    /// Tests for Script loading, stream handling, and file operations.
    /// Targets uncovered branches in Script.cs loading/dump paths.
    /// </summary>
    public sealed class ScriptLoadTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task LoadFunctionWithExplicitFriendlyName()
        {
            Script script = new(CoreModulePresets.Complete);

            DynValue result = script.LoadFunction(
                "function() return 42 end",
                globalTable: null,
                funcFriendlyName: "custom_func_name"
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Function).ConfigureAwait(false);

            DynValue callResult = script.Call(result);
            await Assert.That(callResult.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadFunctionWithNullGlobalTableUsesDefaultGlobals()
        {
            Script script = new(CoreModulePresets.Complete);

            DynValue result = script.LoadFunction("function() return 'ok' end", globalTable: null);

            await Assert.That(result.Type).IsEqualTo(DataType.Function).ConfigureAwait(false);

            DynValue callResult = script.Call(result);
            await Assert.That(callResult.String).IsEqualTo("ok").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadStringWithNullCodeThrows()
        {
            Script script = new(CoreModulePresets.Complete);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.LoadString(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("code").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadStreamWithNullStreamThrows()
        {
            Script script = new(CoreModulePresets.Complete);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.LoadStream(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("stream").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileWithNullFilenameThrows()
        {
            Script script = new(CoreModulePresets.Complete);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.LoadFile(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("filename").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileWithByteArrayLoader()
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes("return 99");
            ByteArrayScriptLoader loader = new(bytes);
            ScriptOptions options = new() { ScriptLoader = loader };
            Script script = new(CoreModulePresets.Complete, options);

            DynValue result = script.LoadFile("dummy.lua");
            DynValue callResult = script.Call(result);

            await Assert.That(callResult.Number).IsEqualTo(99d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileWithStreamLoader()
        {
            string code = "return 123";
            StreamScriptLoader loader = new(code);
            ScriptOptions options = new() { ScriptLoader = loader };
            Script script = new(CoreModulePresets.Complete, options);

            DynValue result = script.LoadFile("dummy.lua");
            DynValue callResult = script.Call(result);

            await Assert.That(callResult.Number).IsEqualTo(123d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileWithNullReturnThrows()
        {
            NullReturningScriptLoader loader = new();
            ScriptOptions options = new() { ScriptLoader = loader };
            Script script = new(CoreModulePresets.Complete, options);

            InvalidCastException exception = Assert.Throws<InvalidCastException>(() =>
                script.LoadFile("test.lua")
            );

            await Assert.That(exception.Message).Contains("null").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileWithUnsupportedTypeThrows()
        {
            UnsupportedTypeScriptLoader loader = new();
            ScriptOptions options = new() { ScriptLoader = loader };
            Script script = new(CoreModulePresets.Complete, options);

            InvalidCastException exception = Assert.Throws<InvalidCastException>(() =>
                script.LoadFile("test.lua")
            );

            await Assert
                .That(exception.Message)
                .Contains("Unsupported return type")
                .ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("Int32").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DumpWithNullFunctionThrows()
        {
            Script script = new(CoreModulePresets.Complete);
            using MemoryStream ms = new();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.Dump(null, ms)
            );

            await Assert.That(exception.ParamName).IsEqualTo("function").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DumpWithNullStreamThrows()
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue func = script.LoadString("return 1");

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.Dump(func, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("stream").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DumpWithNonFunctionThrows()
        {
            Script script = new(CoreModulePresets.Complete);
            using MemoryStream ms = new();
            DynValue notFunction = DynValue.NewNumber(42);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Dump(notFunction, ms)
            );

            await Assert.That(exception.Message).Contains("not a function").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DumpWithReadOnlyStreamThrows()
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue func = script.LoadString("return 1");
            using ReadOnlyMemoryStream roStream = new();

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Dump(func, roStream)
            );

            await Assert.That(exception.Message).Contains("readonly").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DumpWithClosureUpvaluesThrows()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString(
                @"
                local captured = 10
                withCapture = function() return captured end
            "
            );
            DynValue func = script.Globals.Get("withCapture");
            using MemoryStream ms = new();

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Dump(func, ms)
            );

            await Assert.That(exception.Message).Contains("upvalues").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetTypeMetatableWithInvalidTypeThrows()
        {
            Script script = new(CoreModulePresets.Complete);
            Table metatable = new(script);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.SetTypeMetatable((DataType)999, metatable)
            );

            await Assert.That(exception.Message).Contains("not supported").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetTypeMetatableWithInvalidTypeReturnsNull()
        {
            Script script = new(CoreModulePresets.Complete);

            Table result = script.GetTypeMetatable((DataType)999);

            await Assert.That(result).IsNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AttachDebuggerWithNullThrows()
        {
            Script script = new(CoreModulePresets.Complete);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                script.AttachDebugger(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("debugger").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AttachDebuggerSignalsSourceAndByteCode()
        {
            Script script = new(CoreModulePresets.Complete);
            int initialSourceCount = script.SourceCodeCount;
            script.LoadString("return 1", codeFriendlyName: "chunk1");
            script.LoadString("return 2", codeFriendlyName: "chunk2");

            RecordingDebugger debugger = new();
            script.AttachDebugger(debugger);

            // Debugger receives all sources loaded so far (initial + 2 we added)
            await Assert
                .That(debugger.SourceCodeSetCount)
                .IsEqualTo(initialSourceCount + 2)
                .ConfigureAwait(false);
            await Assert.That(debugger.ByteCodeSetCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(script.DebuggerEnabled).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadStringSignalsByteCodeChangeWhenDebuggerAttached()
        {
            Script script = new(CoreModulePresets.Complete);
            RecordingDebugger debugger = new();
            script.AttachDebugger(debugger);

            int initialCount = debugger.ByteCodeSetCount;

            script.LoadString("return 42");

            await Assert
                .That(debugger.ByteCodeSetCount)
                .IsGreaterThan(initialCount)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CreateDynamicExpressionRemovesSourceOnError()
        {
            Script script = new(CoreModulePresets.Complete);
            int beforeCount = script.SourceCodeCount;

            Assert.Throws<SyntaxErrorException>(() => script.CreateDynamicExpression("+++"));

            await Assert.That(script.SourceCodeCount).IsEqualTo(beforeCount).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CreateConstantDynamicExpressionValidatesOwnership()
        {
            Script scriptA = new(CoreModulePresets.Complete);
            Script scriptB = new(CoreModulePresets.Complete);
            DynValue foreignValue = scriptA.DoString("return {}");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptB.CreateConstantDynamicExpression("constant", foreignValue)
            );

            await Assert
                .That(exception.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task WarmUpInitializesLexerParser()
        {
            Script.WarmUp();
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetBannerIncludesVersion()
        {
            string banner = Script.GetBanner();

            await Assert.That(banner).Contains("NovaSharp").ConfigureAwait(false);
            await Assert.That(banner).Contains(Script.VERSION).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetBannerWithSubproductIncludesSubproduct()
        {
            string banner = Script.GetBanner("TestProduct");

            await Assert.That(banner).Contains("TestProduct").ConfigureAwait(false);
            await Assert.That(banner).Contains("NovaSharp").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OwnerScriptReturnsSelf()
        {
            Script script = new(CoreModulePresets.Complete);

            await Assert.That(script.OwnerScript).IsSameReferenceAs(script).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetSourceCodeReturnsCorrectSource()
        {
            Script script = new(CoreModules.Basic);
            int initialSourceCount = script.SourceCodeCount;
            script.LoadString("return 1", codeFriendlyName: "source_one");
            script.LoadString("return 2", codeFriendlyName: "source_two");

            SourceCode source0 = script.GetSourceCode(initialSourceCount);
            SourceCode source1 = script.GetSourceCode(initialSourceCount + 1);

            await Assert.That(source0.Name).IsEqualTo("source_one").ConfigureAwait(false);
            await Assert.That(source1.Name).IsEqualTo("source_two").ConfigureAwait(false);
            await Assert
                .That(script.SourceCodeCount)
                .IsEqualTo(initialSourceCount + 2)
                .ConfigureAwait(false);
        }

        // Helper classes for testing

        private sealed class ByteArrayScriptLoader : ScriptLoaderBase
        {
            private readonly byte[] _bytes;

            public ByteArrayScriptLoader(byte[] bytes)
            {
                _bytes = bytes;
            }

            public override object LoadFile(string file, Table globalContext) => _bytes;

            public override bool ScriptFileExists(string name) => true;
        }

        private sealed class StreamScriptLoader : ScriptLoaderBase
        {
            private readonly string _code;

            public StreamScriptLoader(string code)
            {
                _code = code;
            }

            public override object LoadFile(string file, Table globalContext)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(_code);
                return new MemoryStream(bytes);
            }

            public override bool ScriptFileExists(string name) => true;
        }

        private sealed class NullReturningScriptLoader : ScriptLoaderBase
        {
            public override object LoadFile(string file, Table globalContext) => null;

            public override bool ScriptFileExists(string name) => true;
        }

        private sealed class UnsupportedTypeScriptLoader : ScriptLoaderBase
        {
            public override object LoadFile(string file, Table globalContext) => 123;

            public override bool ScriptFileExists(string name) => true;
        }

        private sealed class ReadOnlyMemoryStream : MemoryStream
        {
            public override bool CanWrite => false;
        }

        private sealed class RecordingDebugger : IDebugger
        {
            public int SourceCodeSetCount { get; private set; }
            public int ByteCodeSetCount { get; private set; }

            public void SetSourceCode(SourceCode sourceCode)
            {
                SourceCodeSetCount++;
            }

            public void SetByteCode(string[] byteCode)
            {
                ByteCodeSetCount++;
            }

            public DebuggerCaps GetDebuggerCaps() => DebuggerCaps.CanDebugSourceCode;

            public void SetDebugService(DebugService debugService) { }

            public bool IsPauseRequested() => false;

            public DebuggerAction GetAction(int instructionPointer, SourceRef sourceRef) =>
                new() { Action = DebuggerAction.ActionType.Run };

            public void SignalExecutionEnded() { }

            public void Update(WatchType watchType, IEnumerable<WatchItem> watchItems) { }

            public IReadOnlyList<DynamicExpression> GetWatchItems() =>
                Array.Empty<DynamicExpression>();

            public bool SignalRuntimeException(ScriptRuntimeException ex) => false;

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs) { }
        }
    }
}
