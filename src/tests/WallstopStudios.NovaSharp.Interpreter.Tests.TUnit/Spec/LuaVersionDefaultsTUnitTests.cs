namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Tests for <see cref="LuaVersionDefaults"/> centralized version resolution (§8.23 from PLAN.md).
    /// Verifies consistent handling of <see cref="LuaCompatibilityVersion.Latest"/> across the codebase.
    /// </summary>
    public sealed class LuaVersionDefaultsTUnitTests : LuaSpecTestBase
    {
        // ========================================
        // LuaVersionDefaults.CurrentDefault Tests
        // ========================================

        [Test]
        public async Task CurrentDefaultIsNotLatest()
        {
            // CurrentDefault must never be Latest to avoid circular resolution
            LuaCompatibilityVersion current = LuaVersionDefaults.CurrentDefault;
            await Assert
                .That(current)
                .IsNotEqualTo(LuaCompatibilityVersion.Latest)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task CurrentDefaultIsLua54()
        {
            // Currently, the default is Lua 5.4 (the latest stable release)
            LuaCompatibilityVersion current = LuaVersionDefaults.CurrentDefault;
            await Assert
                .That(current)
                .IsEqualTo(LuaCompatibilityVersion.Lua54)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task HighestSupportedIsNotLatest()
        {
            // HighestSupported must never be Latest to avoid circular resolution
            LuaCompatibilityVersion highest = LuaVersionDefaults.HighestSupported;
            await Assert
                .That(highest)
                .IsNotEqualTo(LuaCompatibilityVersion.Latest)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task HighestSupportedIsLua55()
        {
            // The highest supported version includes experimental Lua 5.5 support
            LuaCompatibilityVersion highest = LuaVersionDefaults.HighestSupported;
            await Assert
                .That(highest)
                .IsEqualTo(LuaCompatibilityVersion.Lua55)
                .ConfigureAwait(false);
        }

        // ========================================
        // LuaVersionDefaults.Resolve Tests
        // ========================================

        [Test]
        public async Task ResolveLatestReturnsCurrentDefault()
        {
            LuaCompatibilityVersion result = LuaVersionDefaults.Resolve(
                LuaCompatibilityVersion.Latest
            );

            await Assert
                .That(result)
                .IsEqualTo(LuaVersionDefaults.CurrentDefault)
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResolveConcreteVersionReturnsUnchanged(LuaCompatibilityVersion version)
        {
            LuaCompatibilityVersion result = LuaVersionDefaults.Resolve(version);

            await Assert.That(result).IsEqualTo(version).ConfigureAwait(false);
        }

        // ========================================
        // LuaVersionDefaults.ResolveForHighest Tests
        // ========================================

        [Test]
        public async Task ResolveForHighestLatestReturnsHighestSupported()
        {
            LuaCompatibilityVersion result = LuaVersionDefaults.ResolveForHighest(
                LuaCompatibilityVersion.Latest
            );

            await Assert
                .That(result)
                .IsEqualTo(LuaVersionDefaults.HighestSupported)
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResolveForHighestConcreteVersionReturnsUnchanged(
            LuaCompatibilityVersion version
        )
        {
            LuaCompatibilityVersion result = LuaVersionDefaults.ResolveForHighest(version);

            await Assert.That(result).IsEqualTo(version).ConfigureAwait(false);
        }

        // ========================================
        // ScriptOptions Default Constructor Tests
        // ========================================

        [Test]
        public async Task ScriptOptionsDefaultConstructorSetsLatest()
        {
            ScriptOptions options = new ScriptOptions();

            await Assert
                .That(options.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Latest)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ScriptOptionsWithLatestBehavesIdenticallyToDefaultScript()
        {
            // Script with explicit Latest
            Script scriptWithLatest = new Script(
                new ScriptOptions(Script.DefaultOptions)
                {
                    CompatibilityVersion = LuaCompatibilityVersion.Latest,
                }
            );

            // Script with no options (default)
            Script scriptDefault = new Script();

            // Both should report the same effective compatibility version
            await Assert
                .That(scriptWithLatest.CompatibilityVersion)
                .IsEqualTo(scriptDefault.CompatibilityVersion)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ScriptWithLatestUsesCorrectRngProvider()
        {
            // Script with explicit Latest
            Script scriptLatest = new Script(
                new ScriptOptions(Script.DefaultOptions)
                {
                    CompatibilityVersion = LuaCompatibilityVersion.Latest,
                }
            );

            // Script with explicit Lua54 (what Latest should resolve to)
            Script scriptLua54 = new Script(
                new ScriptOptions(Script.DefaultOptions)
                {
                    CompatibilityVersion = LuaCompatibilityVersion.Lua54,
                }
            );

            // Both should have the same RNG provider type
            await Assert
                .That(scriptLatest.RandomProvider.GetType())
                .IsEqualTo(scriptLua54.RandomProvider.GetType())
                .ConfigureAwait(false);

            // Both should be LuaRandomProvider (xoshiro256** for 5.4+)
            await Assert
                .That(scriptLatest.RandomProvider)
                .IsTypeOf<LuaRandomProvider>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ScriptWithLatestHasCorrectMathRandomseedBehavior()
        {
            Script script = new Script(
                new ScriptOptions(Script.DefaultOptions)
                {
                    CompatibilityVersion = LuaCompatibilityVersion.Latest,
                }
            );

            // Lua 5.4+ math.randomseed returns the seed tuple
            DynValue result = script.DoString("return math.randomseed(12345)");

            // Should return a tuple (5.4+ behavior)
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
        }

        // ========================================
        // Script Constructor Consistency Tests
        // ========================================

        [Test]
        public async Task AllScriptConstructorsProduceSameCompatibilityVersion()
        {
            // Different constructor overloads
            Script script1 = new Script();
            Script script2 = new Script(CoreModules.PresetComplete);
            Script script3 = new Script(new ScriptOptions(Script.DefaultOptions));
            Script script4 = new Script(
                CoreModules.PresetComplete,
                new ScriptOptions(Script.DefaultOptions)
            );

            // All should have the same compatibility version
            await Assert
                .That(script1.CompatibilityVersion)
                .IsEqualTo(script2.CompatibilityVersion)
                .ConfigureAwait(false);
            await Assert
                .That(script2.CompatibilityVersion)
                .IsEqualTo(script3.CompatibilityVersion)
                .ConfigureAwait(false);
            await Assert
                .That(script3.CompatibilityVersion)
                .IsEqualTo(script4.CompatibilityVersion)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AllScriptConstructorsProduceSameRngType()
        {
            Script script1 = new Script();
            Script script2 = new Script(CoreModules.PresetComplete);
            Script script3 = new Script(new ScriptOptions(Script.DefaultOptions));
            Script script4 = new Script(
                CoreModules.PresetComplete,
                new ScriptOptions(Script.DefaultOptions)
            );

            // All should have the same RNG provider type
            System.Type expectedType = script1.RandomProvider.GetType();
            await Assert
                .That(script2.RandomProvider.GetType())
                .IsEqualTo(expectedType)
                .ConfigureAwait(false);
            await Assert
                .That(script3.RandomProvider.GetType())
                .IsEqualTo(expectedType)
                .ConfigureAwait(false);
            await Assert
                .That(script4.RandomProvider.GetType())
                .IsEqualTo(expectedType)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ScriptWithSameSeedProducesSameFirstRandomValue()
        {
            const int seed = 54321;

            Script script1 = new Script();
            Script script2 = new Script();

            script1.DoString($"math.randomseed({seed})");
            script2.DoString($"math.randomseed({seed})");

            DynValue rand1 = script1.DoString("return math.random()");
            DynValue rand2 = script2.DoString("return math.random()");

            await Assert.That(rand1.Number).IsEqualTo(rand2.Number).ConfigureAwait(false);
        }

        // ========================================
        // Version-specific RNG Selection Tests
        // ========================================

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task OlderVersionsUseLua51RandomProvider(LuaCompatibilityVersion version)
        {
            Script script = new Script(
                new ScriptOptions(Script.DefaultOptions) { CompatibilityVersion = version }
            );

            await Assert
                .That(script.RandomProvider)
                .IsTypeOf<Lua51RandomProvider>()
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task NewerVersionsUseLuaRandomProvider(LuaCompatibilityVersion version)
        {
            Script script = new Script(
                new ScriptOptions(Script.DefaultOptions) { CompatibilityVersion = version }
            );

            await Assert
                .That(script.RandomProvider)
                .IsTypeOf<LuaRandomProvider>()
                .ConfigureAwait(false);
        }

        // ========================================
        // ScriptOptions Copy Constructor Tests
        // ========================================

        [Test]
        public async Task ScriptOptionsCopyConstructorPreservesVersion()
        {
            ScriptOptions original = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua53,
            };
            ScriptOptions copy = new ScriptOptions(original);

            await Assert
                .That(copy.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Lua53)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ScriptOptionsCopyConstructorPreservesLatest()
        {
            ScriptOptions original = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = LuaCompatibilityVersion.Latest,
            };
            ScriptOptions copy = new ScriptOptions(original);

            await Assert
                .That(copy.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Latest)
                .ConfigureAwait(false);
        }
    }
}
