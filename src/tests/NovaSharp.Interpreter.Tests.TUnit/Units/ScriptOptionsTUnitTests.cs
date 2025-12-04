namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Infrastructure;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    /// <summary>
    /// Tests for Script constructor options, global options scopes, and default options handling.
    /// Targets uncovered branches related to TimeProvider, HighResolutionClock, and options scopes.
    /// </summary>
    [ScriptGlobalOptionsIsolation]
    public sealed class ScriptOptionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstructorWithCustomTimeProvider()
        {
            DateTimeOffset fixedTime = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            FakeTimeProvider timeProvider = new(fixedTime);
            ScriptOptions options = new() { TimeProvider = timeProvider };
            Script script = new(CoreModules.PresetComplete, options);

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
            Script script = new(CoreModules.PresetComplete, options);

            await Assert.That(script.PerformanceStats).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorWithNullOptionsUsesDefaults()
        {
            Script script = new(CoreModules.PresetComplete, (ScriptOptions)null);

            await Assert.That(script.Options).IsNotNull().ConfigureAwait(false);
            await Assert.That(script.TimeProvider).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorWithOptionsInheritsCompatibilityVersion()
        {
            ScriptOptions options = new() { CompatibilityVersion = LuaCompatibilityVersion.Lua52 };
            Script script = new(CoreModules.PresetComplete, options);

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

            Script script = new(CoreModules.PresetComplete, (ScriptOptions)null);

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
            Script script = new(CoreModules.PresetComplete);

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
            Script script = new(CoreModules.PresetComplete, options);

            LuaCompatibilityProfile profile = script.CompatibilityProfile;

            await Assert.That(profile.DisplayName).Contains("5.4").ConfigureAwait(false);
            await Assert.That(profile.SupportsBit32Library).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RecycleCoroutineValidatesCoroutineType()
        {
            Script script = new(CoreModules.PresetComplete);
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
            Script script = new(CoreModules.PresetComplete);
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
            Script script = new(CoreModules.PresetComplete);
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
            Script scriptA = new(CoreModules.PresetComplete);
            Script scriptB = new(CoreModules.PresetComplete);

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
            Script script = new(CoreModules.PresetComplete);
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
            Script script = new(CoreModules.PresetComplete);

            Table registry = script.Registry;

            await Assert.That(registry).IsNotNull().ConfigureAwait(false);
            await Assert.That(registry.OwnerScript).IsSameReferenceAs(script).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RunFileCreatesScriptAndExecutes()
        {
            // This requires a real file, so we'll use a temp file
            using TempFileScope tempFile = TempFileScope.CreateWithText(
                "return 42",
                extension: ".lua"
            );

            DynValue result = Script.RunFile(tempFile.FilePath);

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RunStringCreatesScriptAndExecutes()
        {
            DynValue result = Script.RunString("return 'hello'");

            await Assert.That(result.String).IsEqualTo("hello").ConfigureAwait(false);
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
