namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Tests for Script constructor options, global options scopes, and default options handling.
    /// Targets uncovered branches related to TimeProvider, HighResolutionClock, and options scopes.
    /// </summary>
    [ScriptGlobalOptionsIsolation]
    [ScriptDefaultOptionsIsolation]
    [PlatformDetectorIsolation]
    public sealed class ScriptOptionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstructorWithCustomTimeProvider()
        {
            DateTimeOffset fixedTime = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            FakeTimeProvider timeProvider = new(fixedTime);
            ScriptOptions options = new() { TimeProvider = timeProvider };
            Script script = new(CoreModulePresets.Complete, options);

            await Assert
                .That(script.TimeProvider)
                .IsSameReferenceAs(timeProvider)
                .ConfigureAwait(false);
            await Assert
                .That(script.StartTimeUtc)
                .IsEqualTo(fixedTime.UtcDateTime)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorWithCustomHighResolutionClock()
        {
            FakeHighResolutionClock clock = new(12345L);
            ScriptOptions options = new() { HighResolutionClock = clock };
            Script script = new(CoreModulePresets.Complete, options);

            await Assert.That(script.PerformanceStats).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorWithNullOptionsUsesDefaults()
        {
            Script script = new(CoreModulePresets.Complete, (ScriptOptions)null);

            await Assert.That(script.Options).IsNotNull().ConfigureAwait(false);
            await Assert.That(script.TimeProvider).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorWithOptionsInheritsCompatibilityVersion()
        {
            ScriptOptions options = new() { CompatibilityVersion = LuaCompatibilityVersion.Lua52 };
            Script script = new(CoreModulePresets.Complete, options);

            await Assert
                .That(script.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Lua52)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorWithNullOptionsUsesGlobalCompatibilityVersion()
        {
            LuaCompatibilityVersion originalVersion = Script.GlobalOptions.CompatibilityVersion;
            Script.GlobalOptions.CompatibilityVersion = LuaCompatibilityVersion.Lua53;

            Script script = new(CoreModulePresets.Complete, (ScriptOptions)null);

            await Assert
                .That(script.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Lua53)
                .ConfigureAwait(false);

            Script.GlobalOptions.CompatibilityVersion = originalVersion;
        }

        [global::TUnit.Core.Test]
        public async Task DefaultOptionsAreClonedToScripts()
        {
            Script.DefaultOptions.UseLuaErrorLocations = true;
            Script script = new(CoreModulePresets.Complete);

            await Assert.That(script.Options.UseLuaErrorLocations).IsTrue().ConfigureAwait(false);

            Script.DefaultOptions.UseLuaErrorLocations = false;
        }

        [global::TUnit.Core.Test]
        public async Task GlobalOptionsScopeRestoresPreviousValue()
        {
            ScriptGlobalOptions original = Script.GlobalOptions;
            bool originalRethrow = original.RethrowExceptionNested;

            using (Script.BeginGlobalOptionsScope())
            {
                Script.GlobalOptions.RethrowExceptionNested = !originalRethrow;
                await Assert
                    .That(Script.GlobalOptions.RethrowExceptionNested)
                    .IsNotEqualTo(originalRethrow)
                    .ConfigureAwait(false);
            }

            await Assert
                .That(Script.GlobalOptions.RethrowExceptionNested)
                .IsEqualTo(originalRethrow)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GlobalOptionsScopeCreatesClone()
        {
            ScriptGlobalOptions original = Script.GlobalOptions;

            using (Script.BeginGlobalOptionsScope())
            {
                await Assert
                    .That(Script.GlobalOptions)
                    .IsNotSameReferenceAs(original)
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task DefaultOptionsScopeRestoresOnDispose()
        {
            bool originalValue = Script.DefaultOptions.UseLuaErrorLocations;

            using (Script.BeginDefaultOptionsScope())
            {
                Script.DefaultOptions.UseLuaErrorLocations = !originalValue;
                await Assert
                    .That(Script.DefaultOptions.UseLuaErrorLocations)
                    .IsNotEqualTo(originalValue)
                    .ConfigureAwait(false);
            }

            await Assert
                .That(Script.DefaultOptions.UseLuaErrorLocations)
                .IsEqualTo(originalValue)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DefaultOptionsScopeRestoresAllProperties()
        {
            int originalThreshold = Script.DefaultOptions.TailCallOptimizationThreshold;
            bool originalThreadCheck = Script.DefaultOptions.CheckThreadAccess;

            using (Script.BeginDefaultOptionsScope())
            {
                Script.DefaultOptions.TailCallOptimizationThreshold = 99999;
                Script.DefaultOptions.CheckThreadAccess = !originalThreadCheck;
            }

            await Assert
                .That(Script.DefaultOptions.TailCallOptimizationThreshold)
                .IsEqualTo(originalThreshold)
                .ConfigureAwait(false);
            await Assert
                .That(Script.DefaultOptions.CheckThreadAccess)
                .IsEqualTo(originalThreadCheck)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DefaultOptionsScopeHandlesDoubleDispose()
        {
            IDisposable scope = Script.BeginDefaultOptionsScope();
            bool originalValue = Script.DefaultOptions.UseLuaErrorLocations;
            Script.DefaultOptions.UseLuaErrorLocations = !originalValue;

            scope.Dispose();
            scope.Dispose(); // Second dispose should be no-op

            await Assert
                .That(Script.DefaultOptions.UseLuaErrorLocations)
                .IsEqualTo(originalValue)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CompatibilityProfileReturnsCorrectProfile()
        {
            ScriptOptions options = new() { CompatibilityVersion = LuaCompatibilityVersion.Lua54 };
            Script script = new(CoreModulePresets.Complete, options);

            LuaCompatibilityProfile profile = script.CompatibilityProfile;

            await Assert.That(profile.DisplayName).Contains("5.4").ConfigureAwait(false);
            await Assert.That(profile.SupportsBit32Library).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RecycleCoroutineValidatesCoroutineType()
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.NewString("done"));
            DynValue coroutineValue = script.CreateCoroutine(callback);

            // CLR callback coroutines have CoroutineType.ClrCallback, not Coroutine
            DynValue newFunc = script.LoadString("return 'recycled'");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                script.RecycleCoroutine(coroutineValue.Coroutine, newFunc)
            );

            await Assert
                .That(exception.Message)
                .Contains("CoroutineType.Coroutine")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RecycleCoroutineValidatesCoroutineState()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString("function gen() coroutine.yield(1) return 2 end");
            DynValue func = script.Globals.Get("gen");
            DynValue coroutineValue = script.CreateCoroutine(func);

            // Coroutine is in NotStarted state, not Dead
            DynValue newFunc = script.LoadString("return 'new'");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                script.RecycleCoroutine(coroutineValue.Coroutine, newFunc)
            );

            await Assert
                .That(exception.Message)
                .Contains("CoroutineState.Dead")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RecycleCoroutineValidatesFunctionType()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString("function gen() return 'done' end");
            DynValue func = script.Globals.Get("gen");
            DynValue coroutineValue = script.CreateCoroutine(func);
            coroutineValue.Coroutine.Resume(); // Execute to Dead state

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                script.RecycleCoroutine(coroutineValue.Coroutine, null)
            );

            await Assert
                .That(exception.Message)
                .Contains("DataType.Function")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RecycleCoroutineValidatesOwnership()
        {
            Script scriptA = new(CoreModulePresets.Complete);
            Script scriptB = new(CoreModulePresets.Complete);

            scriptA.DoString("function gen() return 'a' end");
            DynValue funcA = scriptA.Globals.Get("gen");
            DynValue coroutineValue = scriptA.CreateCoroutine(funcA);
            coroutineValue.Coroutine.Resume();

            DynValue funcB = scriptB.LoadString("return 'b'");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                scriptA.RecycleCoroutine(coroutineValue.Coroutine, funcB)
            );

            await Assert
                .That(exception.Message)
                .Contains("different scripts")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RecycleCoroutineReusesBuffers()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString("function gen() return 'original' end");
            DynValue originalFunc = script.Globals.Get("gen");
            DynValue coroutineValue = script.CreateCoroutine(originalFunc);
            coroutineValue.Coroutine.Resume(); // Execute to Dead

            script.DoString("function newGen() return 'recycled' end");
            DynValue newFunc = script.Globals.Get("newGen");

            DynValue recycled = script.RecycleCoroutine(coroutineValue.Coroutine, newFunc);
            DynValue result = recycled.Coroutine.Resume();

            await Assert.That(result.String).IsEqualTo("recycled").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebuggerEnabledCanBeToggled()
        {
            Script script = new(CoreModules.Basic);

            // Default is true (set by DebugContext)
            bool initialState = script.DebuggerEnabled;
            await Assert.That(initialState).IsTrue().ConfigureAwait(false);

            script.DebuggerEnabled = false;
            await Assert.That(script.DebuggerEnabled).IsFalse().ConfigureAwait(false);

            script.DebuggerEnabled = true;
            await Assert.That(script.DebuggerEnabled).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RegistryIsAccessible()
        {
            Script script = new(CoreModulePresets.Complete);

            Table registry = script.Registry;

            await Assert.That(registry).IsNotNull().ConfigureAwait(false);
            await Assert.That(registry.OwnerScript).IsSameReferenceAs(script).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RunFileCreatesScriptAndExecutes()
        {
            // Ensure we're using a file system loader since other tests may modify DefaultOptions.ScriptLoader
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();

            // This requires a real file, so we'll use a temp file
            using TempFileScope tempFile = TempFileScope.CreateWithText(
                "return 42",
                extension: ".lua"
            );

            // Capture diagnostic info before running for better debugging on failure
            string loaderType =
                Script.DefaultOptions.ScriptLoader?.GetType().Name ?? "null (no loader)";
            string diagnosticContext =
                $"FilePath: {tempFile.FilePath}, LoaderType: {loaderType}, "
                + $"FileExists: {System.IO.File.Exists(tempFile.FilePath)}";

            DynValue result = Script.RunFile(tempFile.FilePath);

            await Assert
                .That(result.Number)
                .IsEqualTo(42d)
                .Because($"Expected return value of 42. {diagnosticContext}")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RunStringCreatesScriptAndExecutes()
        {
            DynValue result = Script.RunString("return 'hello'");

            await Assert.That(result.String).IsEqualTo("hello").ConfigureAwait(false);
        }

        /// <summary>
        /// Data-driven test for <see cref="Script.RunFile"/> with various return values.
        /// </summary>
        [global::TUnit.Core.Test]
        [Arguments("return 42", DataType.Number, 42.0, null)]
        [Arguments("return 'test'", DataType.String, null, "test")]
        [Arguments("return true", DataType.Boolean, null, null)]
        [Arguments("return nil", DataType.Nil, null, null)]
        [Arguments("return 1 + 2 * 3", DataType.Number, 7.0, null)]
        public async Task RunFileReturnsExpectedValueDataDriven(
            string code,
            DataType expectedType,
            double? expectedNumber,
            string expectedString
        )
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope tempFile = TempFileScope.CreateWithText(code, extension: ".lua");

            DynValue result = Script.RunFile(tempFile.FilePath);

            await Assert
                .That(result.Type)
                .IsEqualTo(expectedType)
                .Because($"Code: {code}")
                .ConfigureAwait(false);

            if (expectedNumber.HasValue)
            {
                await Assert
                    .That(result.Number)
                    .IsEqualTo(expectedNumber.Value)
                    .ConfigureAwait(false);
            }

            if (expectedString != null)
            {
                await Assert.That(result.String).IsEqualTo(expectedString).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Data-driven test for <see cref="Script.RunString"/> with various return values.
        /// </summary>
        [global::TUnit.Core.Test]
        [Arguments("return 42", DataType.Number, 42.0, null)]
        [Arguments("return 'hello'", DataType.String, null, "hello")]
        [Arguments("return true", DataType.Boolean, null, null)]
        [Arguments("return false", DataType.Boolean, null, null)]
        [Arguments("return nil", DataType.Nil, null, null)]
        [Arguments("return 'foo' .. 'bar'", DataType.String, null, "foobar")]
        [Arguments("return 10 / 2", DataType.Number, 5.0, null)]
        public async Task RunStringReturnsExpectedValueDataDriven(
            string code,
            DataType expectedType,
            double? expectedNumber,
            string expectedString
        )
        {
            DynValue result = Script.RunString(code);

            await Assert
                .That(result.Type)
                .IsEqualTo(expectedType)
                .Because($"Code: {code}")
                .ConfigureAwait(false);

            if (expectedNumber.HasValue)
            {
                await Assert
                    .That(result.Number)
                    .IsEqualTo(expectedNumber.Value)
                    .ConfigureAwait(false);
            }

            if (expectedString != null)
            {
                await Assert.That(result.String).IsEqualTo(expectedString).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Verifies that Script.RunString works regardless of DefaultOptions.ScriptLoader type.
        /// RunString doesn't load files, so it should work with any loader.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task RunStringWorksIndependentOfScriptLoaderType()
        {
            // Capture current loader type for diagnostics
            string loaderType =
                Script.DefaultOptions.ScriptLoader?.GetType().Name ?? "null (no loader)";

            DynValue result = Script.RunString("return 123");

            await Assert
                .That(result.Number)
                .IsEqualTo(123d)
                .Because($"RunString should work regardless of loader type ({loaderType})")
                .ConfigureAwait(false);
        }

        // Helper classes

        private sealed class FakeTimeProvider : ITimeProvider
        {
            private readonly DateTimeOffset _fixedTime;

            public FakeTimeProvider(DateTimeOffset fixedTime)
            {
                _fixedTime = fixedTime;
            }

            public DateTimeOffset GetUtcNow() => _fixedTime;
        }

        private sealed class FakeHighResolutionClock : IHighResolutionClock
        {
            private readonly long _fixedTicks;

            public FakeHighResolutionClock(long fixedTicks)
            {
                _fixedTicks = fixedTicks;
            }

            public long GetTimestamp() => _fixedTicks;

            public double TimestampFrequency => 10_000_000d;

            public long GetElapsedMilliseconds(long startTimestamp, long? endTimestamp = null)
            {
                long end = endTimestamp ?? _fixedTicks;
                return (long)((end - startTimestamp) * 1000.0 / TimestampFrequency);
            }
        }
    }
}
