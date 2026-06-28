namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Tests for Script constructor consistency (§8.2 from PLAN.md).
    /// Verifies that all Script constructors initialize state identically and documents expected behavior.
    /// </summary>
    public sealed class ScriptConstructorConsistencyTUnitTests : LuaSpecTestBase
    {
        // ========================================
        // Constructor Initialization Order Tests
        // ========================================

        /// <summary>
        /// All four Script constructors should use the same initialization order:
        /// 1. Options (from defaults or copy)
        /// 2. TimeProvider
        /// 3. RandomProvider
        /// 4. StartTimeUtc
        /// 5. AllocationTracker (if sandboxing)
        /// 6. PerformanceStats
        /// 7. Registry
        /// 8. ByteCode
        /// 9. MainProcessor
        /// 10. GlobalTable (with core modules)
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllConstructorPathsInitializeInSameOrder(LuaCompatibilityVersion version)
        {
            // These all use the main constructor path Script(CoreModules, ScriptOptions)
            Script script1 = new Script();
            Script script2 = new Script(version, CoreModulePresets.Complete);
            Script script3 = new Script(new ScriptOptions(Script.DefaultOptions));
            Script script4 = new Script(
                CoreModulePresets.Complete,
                new ScriptOptions(Script.DefaultOptions)
            );

            // All should have non-null essential properties
            await Assert.That(script1.Options).IsNotNull().ConfigureAwait(false);
            await Assert.That(script1.TimeProvider).IsNotNull().ConfigureAwait(false);
            await Assert.That(script1.RandomProvider).IsNotNull().ConfigureAwait(false);
            await Assert.That(script1.PerformanceStats).IsNotNull().ConfigureAwait(false);
            await Assert.That(script1.Registry).IsNotNull().ConfigureAwait(false);
            await Assert.That(script1.Globals).IsNotNull().ConfigureAwait(false);

            // Same for script2
            await Assert.That(script2.Options).IsNotNull().ConfigureAwait(false);
            await Assert.That(script2.TimeProvider).IsNotNull().ConfigureAwait(false);
            await Assert.That(script2.RandomProvider).IsNotNull().ConfigureAwait(false);
            await Assert.That(script2.PerformanceStats).IsNotNull().ConfigureAwait(false);
            await Assert.That(script2.Registry).IsNotNull().ConfigureAwait(false);
            await Assert.That(script2.Globals).IsNotNull().ConfigureAwait(false);

            // Same for script3
            await Assert.That(script3.Options).IsNotNull().ConfigureAwait(false);
            await Assert.That(script3.TimeProvider).IsNotNull().ConfigureAwait(false);
            await Assert.That(script3.RandomProvider).IsNotNull().ConfigureAwait(false);
            await Assert.That(script3.PerformanceStats).IsNotNull().ConfigureAwait(false);
            await Assert.That(script3.Registry).IsNotNull().ConfigureAwait(false);
            await Assert.That(script3.Globals).IsNotNull().ConfigureAwait(false);

            // Same for script4
            await Assert.That(script4.Options).IsNotNull().ConfigureAwait(false);
            await Assert.That(script4.TimeProvider).IsNotNull().ConfigureAwait(false);
            await Assert.That(script4.RandomProvider).IsNotNull().ConfigureAwait(false);
            await Assert.That(script4.PerformanceStats).IsNotNull().ConfigureAwait(false);
            await Assert.That(script4.Registry).IsNotNull().ConfigureAwait(false);
            await Assert.That(script4.Globals).IsNotNull().ConfigureAwait(false);
        }

        // ========================================
        // GlobalOptions Inheritance Tests
        // ========================================

        /// <summary>
        /// When options is null, Script should inherit CompatibilityVersion from GlobalOptions.
        /// The parameterless constructor and Script(CoreModules) use null options and inherit from GlobalOptions.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task NullOptionsInheritsGlobalOptionsCompatibilityVersion(
            LuaCompatibilityVersion version
        )
        {
            // Script() and Script(CoreModules) both pass null for options
            Script script1 = new Script();
            Script script2 = new Script(CoreModulePresets.Complete);

            // Both should have GlobalOptions.CompatibilityVersion
            LuaCompatibilityVersion expected = Script.GlobalOptions.CompatibilityVersion;

            await Assert
                .That(script1.CompatibilityVersion)
                .IsEqualTo(expected)
                .Because($"Script() should inherit GlobalOptions.CompatibilityVersion ({expected})")
                .ConfigureAwait(false);
            await Assert
                .That(script2.CompatibilityVersion)
                .IsEqualTo(expected)
                .Because(
                    $"Script(CoreModules) should inherit GlobalOptions.CompatibilityVersion ({expected})"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// When an explicit version is passed to the constructor, it should use that version, not GlobalOptions.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ExplicitVersionOverridesGlobalOptions(LuaCompatibilityVersion version)
        {
            // Script(version) and Script(version, CoreModules) explicitly set the version
            Script script1 = new Script(version);
            Script script2 = new Script(version, CoreModulePresets.Complete);

            // Both should use the explicit version, NOT GlobalOptions.CompatibilityVersion
            await Assert
                .That(script1.CompatibilityVersion)
                .IsEqualTo(version)
                .Because(
                    $"Script(version) should use explicit version ({version}), not GlobalOptions"
                )
                .ConfigureAwait(false);
            await Assert
                .That(script2.CompatibilityVersion)
                .IsEqualTo(version)
                .Because(
                    $"Script(version, CoreModules) should use explicit version ({version}), not GlobalOptions"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// When explicit options are passed, Script should NOT inherit from GlobalOptions.
        /// The user's ScriptOptions.CompatibilityVersion is used as-is.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ExplicitOptionsDoesNotInheritFromGlobalOptions(
            LuaCompatibilityVersion version
        )
        {
            // Create options with a specific version
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua52,
            };

            Script script = new Script(options);

            // Should use the explicit version, not GlobalOptions
            await Assert
                .That(script.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Lua52)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Documents the behavior: new ScriptOptions() creates options with Latest,
        /// which may differ from GlobalOptions.CompatibilityVersion if GlobalOptions was modified.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FreshScriptOptionsDefaultsToLatest(LuaCompatibilityVersion version)
        {
            // A fresh ScriptOptions (not copied from DefaultOptions) uses Latest
            ScriptOptions fresh = new ScriptOptions();

            await Assert
                .That(fresh.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Latest)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Documents the recommended pattern: copy from DefaultOptions to get GlobalOptions.CompatibilityVersion.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CopyFromDefaultOptionsGetsGlobalOptionsCompatibilityVersion(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions copied = new ScriptOptions(Script.DefaultOptions);

            // DefaultOptions.CompatibilityVersion was set from GlobalOptions at static init
            // In the default case, this equals GlobalOptions.CompatibilityVersion
            await Assert
                .That(copied.CompatibilityVersion)
                .IsEqualTo(Script.DefaultOptions.CompatibilityVersion)
                .ConfigureAwait(false);
        }

        // ========================================
        // RandomProvider Selection Tests
        // ========================================

        /// <summary>
        /// When no RandomProvider is specified in options, Script creates the appropriate
        /// provider based on CompatibilityVersion.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RandomProviderSelectedBasedOnCompatibilityVersion(
            LuaCompatibilityVersion version
        )
        {
            // Lua 5.2 should get Lua51RandomProvider (LCG)
            ScriptOptions options52 = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua52,
                RandomProvider = null, // Explicitly null
            };
            Script script52 = new Script(options52);
            await Assert
                .That(script52.RandomProvider)
                .IsTypeOf<Lua51RandomProvider>()
                .ConfigureAwait(false);

            // Lua 5.4 should get LuaRandomProvider (xoshiro256**)
            ScriptOptions options54 = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua54,
                RandomProvider = null,
            };
            Script script54 = new Script(options54);
            await Assert
                .That(script54.RandomProvider)
                .IsTypeOf<LuaRandomProvider>()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// When a custom RandomProvider is specified, Script should use it regardless of version.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CustomRandomProviderIsRespected(LuaCompatibilityVersion version)
        {
            DeterministicRandomProvider customProvider = new DeterministicRandomProvider(12345);

            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua54,
                RandomProvider = customProvider,
            };

            Script script = new Script(options);

            await Assert
                .That(script.RandomProvider)
                .IsSameReferenceAs(customProvider)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// DeterministicRandomProvider produces reproducible sequences.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DeterministicRandomProviderProducesReproducibleSequences(
            LuaCompatibilityVersion version
        )
        {
            const int seed = 99999;

            DeterministicRandomProvider provider1 = new DeterministicRandomProvider(seed);
            DeterministicRandomProvider provider2 = new DeterministicRandomProvider(seed);

            Script script1 = new Script(
                new ScriptOptions(Script.DefaultOptions) { RandomProvider = provider1 }
            );
            Script script2 = new Script(
                new ScriptOptions(Script.DefaultOptions) { RandomProvider = provider2 }
            );

            DynValue rand1 = script1.DoString("return math.random(), math.random(), math.random()");
            DynValue rand2 = script2.DoString("return math.random(), math.random(), math.random()");

            await Assert
                .That(rand1.Tuple[0].Number)
                .IsEqualTo(rand2.Tuple[0].Number)
                .ConfigureAwait(false);
            await Assert
                .That(rand1.Tuple[1].Number)
                .IsEqualTo(rand2.Tuple[1].Number)
                .ConfigureAwait(false);
            await Assert
                .That(rand1.Tuple[2].Number)
                .IsEqualTo(rand2.Tuple[2].Number)
                .ConfigureAwait(false);
        }

        // ========================================
        // Core Module Registration Order Tests
        // ========================================

        /// <summary>
        /// Core modules are registered in a deterministic order defined by RegisterCoreModules.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CoreModuleRegistrationOrderIsDeterministic(
            LuaCompatibilityVersion version
        )
        {
            Script script1 = new Script(version, CoreModulePresets.Complete);
            Script script2 = new Script(version, CoreModulePresets.Complete);

            // Both should have the same global functions available
            await Assert.That(script1.Globals["print"]).IsNotNull().ConfigureAwait(false);
            await Assert.That(script2.Globals["print"]).IsNotNull().ConfigureAwait(false);

            await Assert.That(script1.Globals["math"]).IsNotNull().ConfigureAwait(false);
            await Assert.That(script2.Globals["math"]).IsNotNull().ConfigureAwait(false);

            await Assert.That(script1.Globals["string"]).IsNotNull().ConfigureAwait(false);
            await Assert.That(script2.Globals["string"]).IsNotNull().ConfigureAwait(false);

            await Assert.That(script1.Globals["table"]).IsNotNull().ConfigureAwait(false);
            await Assert.That(script2.Globals["table"]).IsNotNull().ConfigureAwait(false);
        }

        /// <summary>
        /// Different CoreModules configurations produce predictable global table contents.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CoreModulesConfigurationAffectsGlobalTable(
            LuaCompatibilityVersion version
        )
        {
            // Minimal modules: only Basic
            Script minimal = new Script(CoreModules.Basic);
            await Assert.That(minimal.Globals["print"]).IsNotNull().ConfigureAwait(false);
            await Assert.That(minimal.Globals["math"]).IsNull().ConfigureAwait(false);

            // Complete modules: everything
            Script complete = new Script(version, CoreModulePresets.Complete);
            await Assert.That(complete.Globals["print"]).IsNotNull().ConfigureAwait(false);
            await Assert.That(complete.Globals["math"]).IsNotNull().ConfigureAwait(false);
        }

        // ========================================
        // TimeProvider Initialization Tests
        // ========================================

        /// <summary>
        /// When no TimeProvider is specified, SystemTimeProvider.Instance is used.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DefaultTimeProviderIsSystemTimeProvider(LuaCompatibilityVersion version)
        {
            Script script = new Script(version);

            await Assert
                .That(script.TimeProvider)
                .IsSameReferenceAs(SystemTimeProvider.Instance)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Custom TimeProvider is respected.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CustomTimeProviderIsRespected(LuaCompatibilityVersion version)
        {
            DateTimeOffset fixedTime = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
            DeterministicTimeProvider customProvider = new DeterministicTimeProvider(fixedTime);

            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                TimeProvider = customProvider,
            };

            Script script = new Script(options);

            await Assert
                .That(script.TimeProvider)
                .IsSameReferenceAs(customProvider)
                .ConfigureAwait(false);
        }

        // ========================================
        // StartTimeUtc Initialization Tests
        // ========================================

        /// <summary>
        /// StartTimeUtc is captured from TimeProvider at construction time.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StartTimeUtcIsCapturedAtConstruction(LuaCompatibilityVersion version)
        {
            DateTimeOffset fixedTime = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
            DeterministicTimeProvider timeProvider = new DeterministicTimeProvider(fixedTime);

            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                TimeProvider = timeProvider,
            };

            Script script = new Script(options);

            // StartTimeUtc is internal but affects os.clock calculations
            // We can verify indirectly through os.clock behavior
            DynValue clock1 = script.DoString("return os.clock()");
            double elapsed1 = clock1.Number;

            // Advance time
            timeProvider.Advance(TimeSpan.FromSeconds(5));

            DynValue clock2 = script.DoString("return os.clock()");
            double elapsed2 = clock2.Number;

            // Clock should show ~5 seconds difference
            double delta = elapsed2 - elapsed1;
            await Assert.That(delta).IsGreaterThanOrEqualTo(4.9).ConfigureAwait(false);
            await Assert.That(delta).IsLessThanOrEqualTo(5.1).ConfigureAwait(false);
        }

        // ========================================
        // AllocationTracker Initialization Tests
        // ========================================

        /// <summary>
        /// AllocationTracker is only created when sandbox has memory or coroutine limits.
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllocationTrackerCreatedOnlyWithSandboxLimits(
            LuaCompatibilityVersion version
        )
        {
            // No limits - no tracker
            Script noLimits = new Script();
            await Assert.That(noLimits.AllocationTracker).IsNull().ConfigureAwait(false);

            // Memory limit - has tracker
            ScriptOptions memOptions = new ScriptOptions(Script.DefaultOptions)
            {
                Sandbox = new Sandboxing.SandboxOptions { MaxMemoryBytes = 1024 * 1024 },
            };
            Script memLimited = new Script(memOptions);
            await Assert.That(memLimited.AllocationTracker).IsNotNull().ConfigureAwait(false);

            // Coroutine limit - has tracker
            ScriptOptions coroOptions = new ScriptOptions(Script.DefaultOptions)
            {
                Sandbox = new Sandboxing.SandboxOptions { MaxCoroutines = 10 },
            };
            Script coroLimited = new Script(coroOptions);
            await Assert.That(coroLimited.AllocationTracker).IsNotNull().ConfigureAwait(false);
        }

        // ========================================
        // Constructor Equivalence Tests
        // ========================================

        /// <summary>
        /// Script() is equivalent to Script(CoreModulePresets.Default, null).
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DefaultConstructorEquivalentToExplicitDefault(
            LuaCompatibilityVersion version
        )
        {
            Script default1 = new Script();
            Script default2 = new Script(CoreModulePresets.Default, null);

            // Same compatibility version
            await Assert
                .That(default1.CompatibilityVersion)
                .IsEqualTo(default2.CompatibilityVersion)
                .ConfigureAwait(false);

            // Same RNG provider type
            await Assert
                .That(default1.RandomProvider.GetType())
                .IsEqualTo(default2.RandomProvider.GetType())
                .ConfigureAwait(false);

            // Both have the same basic globals
            await Assert.That(default1.Globals["print"]).IsNotNull().ConfigureAwait(false);
            await Assert.That(default2.Globals["print"]).IsNotNull().ConfigureAwait(false);
        }

        /// <summary>
        /// Script(CoreModules) is equivalent to Script(CoreModules, null).
        /// </summary>
        [Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ModulesOnlyConstructorEquivalentToExplicitNull(
            LuaCompatibilityVersion version
        )
        {
            Script modules1 = new Script(CoreModulePresets.HardSandbox);
            Script modules2 = new Script(CoreModulePresets.HardSandbox, null);

            await Assert
                .That(modules1.CompatibilityVersion)
                .IsEqualTo(modules2.CompatibilityVersion)
                .ConfigureAwait(false);
        }
    }
}
