namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Sandbox
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Tests for deterministic execution mode using <see cref="DeterministicRandomProvider"/>
    /// and <see cref="DeterministicTimeProvider"/>.
    /// </summary>
    public sealed class DeterministicExecutionTUnitTests
    {
        // DeterministicRandomProvider Unit Tests

        [Test]
        public async Task DeterministicRandomProviderDefaultSeedXIs42()
        {
            long defaultSeedX = DeterministicRandomProvider.DefaultSeedX;
            await Assert.That(defaultSeedX).IsEqualTo(42L).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicRandomProviderDefaultConstructorUsesSeed42()
        {
            DeterministicRandomProvider provider = new DeterministicRandomProvider();

            await Assert.That(provider.SeedX).IsEqualTo(42L).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicRandomProviderCustomSeedIsRecorded()
        {
            DeterministicRandomProvider provider = new DeterministicRandomProvider(12345);

            await Assert.That(provider.SeedX).IsEqualTo(12345L).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicRandomProviderProducesRepeatableSequence()
        {
            DeterministicRandomProvider provider1 = new DeterministicRandomProvider(100);
            DeterministicRandomProvider provider2 = new DeterministicRandomProvider(100);

            List<int> sequence1 = new List<int>();
            List<int> sequence2 = new List<int>();

            for (int i = 0; i < 100; i++)
            {
                sequence1.Add(provider1.NextInt());
                sequence2.Add(provider2.NextInt());
            }

            await Assert.That(sequence1).IsEquivalentTo(sequence2).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicRandomProviderSetSeedResetsSequence()
        {
            DeterministicRandomProvider provider = new DeterministicRandomProvider(100);

            int first1 = provider.NextInt();
            int second1 = provider.NextInt();

            provider.SetSeed(100);

            int first2 = provider.NextInt();
            int second2 = provider.NextInt();

            await Assert.That(first1).IsEqualTo(first2).ConfigureAwait(false);
            await Assert.That(second1).IsEqualTo(second2).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicRandomProviderSetSeedUpdatesSeedX()
        {
            DeterministicRandomProvider provider = new DeterministicRandomProvider(100);

            provider.SetSeed(999);

            await Assert.That(provider.SeedX).IsEqualTo(999L).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicRandomProviderNextIntMaxValueRespectsLimit()
        {
            DeterministicRandomProvider provider = new DeterministicRandomProvider();

            for (int i = 0; i < 1000; i++)
            {
                int value = provider.NextInt(10);
                await Assert.That(value).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);
                await Assert.That(value).IsLessThan(10).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task DeterministicRandomProviderNextIntMinMaxRespectsRange()
        {
            DeterministicRandomProvider provider = new DeterministicRandomProvider();

            for (int i = 0; i < 1000; i++)
            {
                int value = provider.NextInt(5, 15);
                await Assert.That(value).IsGreaterThanOrEqualTo(5).ConfigureAwait(false);
                await Assert.That(value).IsLessThan(15).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task DeterministicRandomProviderNextDoubleReturnsZeroToOne()
        {
            DeterministicRandomProvider provider = new DeterministicRandomProvider();

            for (int i = 0; i < 1000; i++)
            {
                double value = provider.NextDouble();
                await Assert.That(value).IsGreaterThanOrEqualTo(0.0).ConfigureAwait(false);
                await Assert.That(value).IsLessThan(1.0).ConfigureAwait(false);
            }
        }

        // DeterministicTimeProvider Unit Tests

        [Test]
        public async Task DeterministicTimeProviderDefaultStartTimeIs2020()
        {
            DateTimeOffset expected = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset actual = DeterministicTimeProvider.DefaultStartTime;

            await Assert.That(actual).IsEqualTo(expected).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicTimeProviderDefaultConstructorUsesDefaultStartTime()
        {
            DeterministicTimeProvider provider = new DeterministicTimeProvider();

            await Assert
                .That(provider.GetUtcNow())
                .IsEqualTo(DeterministicTimeProvider.DefaultStartTime)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicTimeProviderCustomStartTimeIsUsed()
        {
            DateTimeOffset customTime = new DateTimeOffset(2025, 6, 15, 12, 30, 45, TimeSpan.Zero);
            DeterministicTimeProvider provider = new DeterministicTimeProvider(customTime);

            await Assert.That(provider.GetUtcNow()).IsEqualTo(customTime).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicTimeProviderSetTimeChangesCurrentTime()
        {
            DeterministicTimeProvider provider = new DeterministicTimeProvider();
            DateTimeOffset newTime = new DateTimeOffset(2030, 3, 20, 8, 0, 0, TimeSpan.Zero);

            provider.SetTime(newTime);

            await Assert.That(provider.GetUtcNow()).IsEqualTo(newTime).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicTimeProviderAdvanceAddsTimeSpan()
        {
            DeterministicTimeProvider provider = new DeterministicTimeProvider();
            DateTimeOffset expected = DeterministicTimeProvider.DefaultStartTime.Add(
                TimeSpan.FromHours(5)
            );

            provider.Advance(TimeSpan.FromHours(5));

            await Assert.That(provider.GetUtcNow()).IsEqualTo(expected).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicTimeProviderAdvanceSecondsWorks()
        {
            DeterministicTimeProvider provider = new DeterministicTimeProvider();
            DateTimeOffset expected = DeterministicTimeProvider.DefaultStartTime.AddSeconds(123.5);

            provider.AdvanceSeconds(123.5);

            await Assert.That(provider.GetUtcNow()).IsEqualTo(expected).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicTimeProviderAdvanceMillisecondsWorks()
        {
            DeterministicTimeProvider provider = new DeterministicTimeProvider();
            DateTimeOffset expected = DeterministicTimeProvider.DefaultStartTime.AddMilliseconds(
                500.25
            );

            provider.AdvanceMilliseconds(500.25);

            await Assert.That(provider.GetUtcNow()).IsEqualTo(expected).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicTimeProviderResetToDefaultStartTime()
        {
            DeterministicTimeProvider provider = new DeterministicTimeProvider();
            provider.Advance(TimeSpan.FromDays(100));

            provider.Reset();

            await Assert
                .That(provider.GetUtcNow())
                .IsEqualTo(DeterministicTimeProvider.DefaultStartTime)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicTimeProviderResetToSpecificTime()
        {
            DeterministicTimeProvider provider = new DeterministicTimeProvider();
            provider.Advance(TimeSpan.FromDays(100));
            DateTimeOffset resetTime = new DateTimeOffset(2015, 1, 1, 0, 0, 0, TimeSpan.Zero);

            provider.Reset(resetTime);

            await Assert.That(provider.GetUtcNow()).IsEqualTo(resetTime).ConfigureAwait(false);
        }

        // Script Integration Tests - RandomProvider

        [Test]
        public async Task ScriptUsesProvidedRandomProvider()
        {
            DeterministicRandomProvider provider = new DeterministicRandomProvider(999);
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                RandomProvider = provider,
            };
            Script script = new Script(CoreModules.PresetDefault, options);

            bool isSame = ReferenceEquals(script.RandomProvider, provider);
            await Assert.That(isSame).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task ScriptCreatesLuaRandomProviderWhenNotProvided()
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                RandomProvider = null,
            };
            Script script = new Script(CoreModules.PresetDefault, options);

            await Assert
                .That(script.RandomProvider)
                .IsTypeOf<LuaRandomProvider>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task MathRandomUsesDeterministicProvider()
        {
            DeterministicRandomProvider provider = new DeterministicRandomProvider(12345);
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                RandomProvider = provider,
            };

            Script script1 = new Script(CoreModules.PresetDefault, options);
            Script script2 = new Script(CoreModules.PresetDefault, options);

            DataTypes.DynValue result1 = script1.DoString("return math.random()");
            provider.SetSeed(12345); // Reset to same seed
            DataTypes.DynValue result2 = script2.DoString("return math.random()");

            await Assert.That(result1.Number).IsEqualTo(result2.Number).ConfigureAwait(false);
        }

        [Test]
        public async Task MathRandomSeedResetsProviderSequence()
        {
            DeterministicRandomProvider provider = new DeterministicRandomProvider(42);
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                RandomProvider = provider,
            };
            Script script = new Script(CoreModules.PresetDefault, options);

            // Get some random values
            script.DoString("r1 = math.random(); r2 = math.random(); r3 = math.random()");
            double r1 = script.Globals.Get("r1").Number;
            double r2 = script.Globals.Get("r2").Number;
            double r3 = script.Globals.Get("r3").Number;

            // Reset seed via Lua
            script.DoString(
                "math.randomseed(42); r1b = math.random(); r2b = math.random(); r3b = math.random()"
            );
            double r1b = script.Globals.Get("r1b").Number;
            double r2b = script.Globals.Get("r2b").Number;
            double r3b = script.Globals.Get("r3b").Number;

            await Assert.That(r1).IsEqualTo(r1b).ConfigureAwait(false);
            await Assert.That(r2).IsEqualTo(r2b).ConfigureAwait(false);
            await Assert.That(r3).IsEqualTo(r3b).ConfigureAwait(false);
        }

        [Test]
        public async Task MathRandomWithRangeIsDeterministic()
        {
            DeterministicRandomProvider provider = new DeterministicRandomProvider(7777);
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                RandomProvider = provider,
            };
            Script script = new Script(CoreModules.PresetDefault, options);

            DataTypes.DynValue result = script.DoString(
                @"
                local values = {}
                for i = 1, 10 do
                    values[i] = math.random(1, 100)
                end
                return table.concat(values, ',')
            "
            );
            string sequence1 = result.String;

            // Reset and do it again
            provider.SetSeed(7777);
            Script script2 = new Script(CoreModules.PresetDefault, options);
            DataTypes.DynValue result2 = script2.DoString(
                @"
                local values = {}
                for i = 1, 10 do
                    values[i] = math.random(1, 100)
                end
                return table.concat(values, ',')
            "
            );
            string sequence2 = result2.String;

            await Assert.That(sequence1).IsEqualTo(sequence2).ConfigureAwait(false);
        }

        // Script Integration Tests - TimeProvider

        [Test]
        public async Task ScriptUsesProvidedTimeProvider()
        {
            DeterministicTimeProvider provider = new DeterministicTimeProvider();
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                TimeProvider = provider,
            };
            Script script = new Script(CoreModules.PresetDefault, options);

            bool isSame = ReferenceEquals(script.TimeProvider, provider);
            await Assert.That(isSame).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task OsClockUsesDeterministicTimeProvider()
        {
            DeterministicTimeProvider provider = new DeterministicTimeProvider();
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                TimeProvider = provider,
            };
            Script script = new Script(CoreModules.PresetDefault, options);

            // os.clock returns time since script start, so advance by a known amount
            provider.AdvanceSeconds(5.5);

            DataTypes.DynValue result = script.DoString("return os.clock()");

            // Should be approximately 5.5 seconds
            await Assert.That(result.Number).IsGreaterThanOrEqualTo(5.4).ConfigureAwait(false);
            await Assert.That(result.Number).IsLessThan(5.6).ConfigureAwait(false);
        }

        [Test]
        public async Task OsTimeUsesDeterministicTimeProvider()
        {
            DateTimeOffset fixedTime = new DateTimeOffset(2020, 6, 15, 12, 0, 0, TimeSpan.Zero);
            DeterministicTimeProvider provider = new DeterministicTimeProvider(fixedTime);
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                TimeProvider = provider,
            };
            Script script = new Script(CoreModules.PresetDefault, options);

            DataTypes.DynValue result = script.DoString("return os.time()");

            // Unix timestamp for 2020-06-15 12:00:00 UTC
            double expectedTimestamp = (
                fixedTime - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)
            ).TotalSeconds;

            await Assert.That(result.Number).IsEqualTo(expectedTimestamp).ConfigureAwait(false);
        }

        // Full Deterministic Execution Tests

        [Test]
        public async Task FullDeterministicExecutionProducesIdenticalResults()
        {
            // Set up identical deterministic providers
            DeterministicRandomProvider randomProvider1 = new DeterministicRandomProvider(123);
            DeterministicTimeProvider timeProvider1 = new DeterministicTimeProvider();

            DeterministicRandomProvider randomProvider2 = new DeterministicRandomProvider(123);
            DeterministicTimeProvider timeProvider2 = new DeterministicTimeProvider();

            ScriptOptions options1 = new ScriptOptions(Script.DefaultOptions)
            {
                RandomProvider = randomProvider1,
                TimeProvider = timeProvider1,
            };
            ScriptOptions options2 = new ScriptOptions(Script.DefaultOptions)
            {
                RandomProvider = randomProvider2,
                TimeProvider = timeProvider2,
            };

            Script script1 = new Script(CoreModules.PresetDefault, options1);
            Script script2 = new Script(CoreModules.PresetDefault, options2);

            // Simulate time advancement in both
            timeProvider1.AdvanceSeconds(10);
            timeProvider2.AdvanceSeconds(10);

            string luaCode =
                @"
                local results = {}
                results[1] = math.random()
                results[2] = math.random(1, 1000)
                results[3] = os.clock()
                results[4] = os.time()
                return table.concat(results, '|')
            ";

            DataTypes.DynValue result1 = script1.DoString(luaCode);
            DataTypes.DynValue result2 = script2.DoString(luaCode);

            await Assert.That(result1.String).IsEqualTo(result2.String).ConfigureAwait(false);
        }

        [Test]
        public async Task DeterministicExecutionAcrossMultipleScripts()
        {
            // Create a shared deterministic random provider
            DeterministicRandomProvider sharedRandom = new DeterministicRandomProvider(42);
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                RandomProvider = sharedRandom,
            };

            // Multiple scripts sharing the same provider will advance the same sequence
            Script script1 = new Script(CoreModules.PresetDefault, options);
            Script script2 = new Script(CoreModules.PresetDefault, options);

            DataTypes.DynValue r1 = script1.DoString("return math.random()");
            DataTypes.DynValue r2 = script2.DoString("return math.random()");

            // They should get different values since they're advancing the same sequence
            await Assert.That(r1.Number).IsNotEqualTo(r2.Number).ConfigureAwait(false);

            // But resetting the seed should make it repeatable
            sharedRandom.SetSeed(42);
            Script script3 = new Script(CoreModules.PresetDefault, options);
            Script script4 = new Script(CoreModules.PresetDefault, options);

            DataTypes.DynValue r3 = script3.DoString("return math.random()");
            DataTypes.DynValue r4 = script4.DoString("return math.random()");

            await Assert.That(r1.Number).IsEqualTo(r3.Number).ConfigureAwait(false);
            await Assert.That(r2.Number).IsEqualTo(r4.Number).ConfigureAwait(false);
        }
    }
}
