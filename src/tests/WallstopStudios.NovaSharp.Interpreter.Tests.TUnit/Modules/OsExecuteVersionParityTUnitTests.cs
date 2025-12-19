namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Platforms;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    /// <summary>
    /// Tests for <c>os.execute</c> version-specific behavior:
    /// <list type="bullet">
    /// <item>Lua 5.1: Returns just exit status code as a number</item>
    /// <item>Lua 5.2+: Returns tuple <c>(true|nil, "exit"|"signal", code)</c></item>
    /// </list>
    /// </summary>
    [ScriptGlobalOptionsIsolation]
    public sealed class OsExecuteVersionParityTUnitTests
    {
        // =============================================================================
        // Lua 5.1: os.execute returns exit status as number
        // =============================================================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task OsExecuteReturnsNumberInLua51(LuaCompatibilityVersion version)
        {
            using StubPlatformAccessor stub = new();
            stub.NextExecuteExitCode = 0;
            using ScriptContext context = CreateScriptContext(stub, version);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('build')");

            // Lua 5.1: Returns just the exit code as a number
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task OsExecuteReturnsNonZeroNumberOnFailureInLua51(
            LuaCompatibilityVersion version
        )
        {
            using StubPlatformAccessor stub = new();
            stub.NextExecuteExitCode = 7;
            using ScriptContext context = CreateScriptContext(stub, version);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('fail')");

            // Lua 5.1: Returns just the exit code as a number
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(7).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task OsExecuteReturnsNegativeOneOnExceptionInLua51(
            LuaCompatibilityVersion version
        )
        {
            using StubPlatformAccessor stub = new();
            stub.ExecuteThrows = true;
            using ScriptContext context = CreateScriptContext(stub, version);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('fail')");

            // Lua 5.1: Platform exceptions return -1
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(-1).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task OsExecuteReturnsNegativeOneOnNotSupportedInLua51(
            LuaCompatibilityVersion version
        )
        {
            using StubPlatformAccessor stub = new();
            stub.ExecuteNotSupported = true;
            using ScriptContext context = CreateScriptContext(stub, version);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('build')");

            // Lua 5.1: Platform not supported returns -1
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(-1).ConfigureAwait(false);
        }

        // =============================================================================
        // Lua 5.2+: os.execute returns tuple (true|nil, "exit"|"signal", code)
        // =============================================================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OsExecuteReturnsTupleInLua52Plus(LuaCompatibilityVersion version)
        {
            using StubPlatformAccessor stub = new();
            stub.NextExecuteExitCode = 0;
            using ScriptContext context = CreateScriptContext(stub, version);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('build')");

            // Lua 5.2+: Returns (true, "exit", 0) on success
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("exit").ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OsExecuteReturnsNilTupleOnFailureInLua52Plus(
            LuaCompatibilityVersion version
        )
        {
            using StubPlatformAccessor stub = new();
            stub.NextExecuteExitCode = 7;
            using ScriptContext context = CreateScriptContext(stub, version);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('fail')");

            // Lua 5.2+: Returns (nil, "exit", code) on non-zero exit
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("exit").ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(7).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OsExecuteReportsSignalWhenExitCodeNegativeInLua52Plus(
            LuaCompatibilityVersion version
        )
        {
            using StubPlatformAccessor stub = new();
            stub.NextExecuteExitCode = -9;
            using ScriptContext context = CreateScriptContext(stub, version);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('terminate')");

            // Lua 5.2+: Returns (nil, "signal", code) for negative exit codes (signals)
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).IsEqualTo("signal").ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(9).ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OsExecuteReturnsNilTupleOnExceptionInLua52Plus(
            LuaCompatibilityVersion version
        )
        {
            using StubPlatformAccessor stub = new();
            stub.ExecuteThrows = true;
            using ScriptContext context = CreateScriptContext(stub, version);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('fail')");

            // Lua 5.2+: Returns (nil, error_message) on platform exceptions
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .Contains("Command failed")
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OsExecuteReportsNotSupportedMessageInLua52Plus(
            LuaCompatibilityVersion version
        )
        {
            using StubPlatformAccessor stub = new();
            stub.ExecuteNotSupported = true;
            using ScriptContext context = CreateScriptContext(stub, version);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('build')");

            // Lua 5.2+: Returns (nil, not_supported_message) on platform not supported
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .Contains("not supported")
                .ConfigureAwait(false);
        }

        // =============================================================================
        // All versions: os.execute() with no args returns true
        // =============================================================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OsExecuteWithNoArgsReturnsTrueAllVersions(LuaCompatibilityVersion version)
        {
            using StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub, version);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute()");

            // All versions: os.execute() with no args returns true (shell available)
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        // =============================================================================
        // Helper methods and types
        // =============================================================================

        private static ScriptContext CreateScriptContext(
            StubPlatformAccessor stub,
            LuaCompatibilityVersion version
        )
        {
            ArgumentNullException.ThrowIfNull(stub);

            ScriptPlatformScope globalScope = ScriptPlatformScope.Override(stub);
            Script script = new(version, CoreModulePresets.Complete);
            script.Options.DebugPrint = _ => { };
            return new ScriptContext(script, globalScope);
        }

        private readonly struct ScriptContext : IDisposable
        {
            private readonly IDisposable _globalScope;

            internal ScriptContext(Script script, IDisposable globalScope)
            {
                ArgumentNullException.ThrowIfNull(script);
                ArgumentNullException.ThrowIfNull(globalScope);
                Script = script;
                _globalScope = globalScope;
            }

            public Script Script { get; }

            public void Dispose()
            {
                _globalScope.Dispose();
            }
        }

        private sealed class StubPlatformAccessor : IPlatformAccessor, IDisposable
        {
            private readonly List<TempFileScope> _tempFiles = new List<TempFileScope>();

            public bool ExecuteThrows { get; set; }
            public bool ExecuteNotSupported { get; set; }
            public int NextExecuteExitCode { get; set; }

            public CoreModules FilterSupportedCoreModules(CoreModules coreModules) => coreModules;

            public string GetEnvironmentVariable(string name) => null;

            public bool IsRunningOnAOT() => false;

            public string GetPlatformName() => "StubPlatform";

            public void DefaultPrint(string content) { }

            public string DefaultInput(string prompt) => string.Empty;

            public Stream OpenFile(
                Script script,
                string filename,
                System.Text.Encoding encoding,
                string mode
            ) => new MemoryStream();

            public Stream GetStandardStream(StandardFileType type) => Stream.Null;

            public string GetTempFileName()
            {
                TempFileScope scope = TempFileScope.Create(createFile: true);
                _tempFiles.Add(scope);
                return scope.FilePath;
            }

            public void ExitFast(int exitCode) =>
                throw new InvalidOperationException("Exit not expected");

            public bool FileExists(string file) => false;

            public void DeleteFile(string file) { }

            public void MoveFile(string src, string dst) { }

            public int ExecuteCommand(string cmdline)
            {
                if (ExecuteNotSupported)
                {
                    throw new PlatformNotSupportedException("Command execution is not supported.");
                }

                if (ExecuteThrows)
                {
                    throw new InvalidOperationException("Command failed");
                }

                return NextExecuteExitCode;
            }

            public void Dispose()
            {
                foreach (TempFileScope scope in _tempFiles)
                {
                    scope.Dispose();
                }
            }
        }
    }
}
