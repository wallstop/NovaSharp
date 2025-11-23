namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Infrastructure;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Options;
    using NovaSharp.Interpreter.Tests.TestUtilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptOptionsTests
    {
        [Test]
        public void DefaultConstructorSeedsBaselineServices()
        {
            ScriptOptions options = new ScriptOptions();

            Assert.Multiple(() =>
            {
                Assert.That(
                    options.CompatibilityVersion,
                    Is.EqualTo(LuaCompatibilityVersion.Latest)
                );
                Assert.That(
                    options.HighResolutionClock,
                    Is.SameAs(SystemHighResolutionClock.Instance)
                );
                Assert.That(options.TimeProvider, Is.SameAs(SystemTimeProvider.Instance));
            });
        }

        [Test]
        public void CopyConstructorClonesConfiguredValues()
        {
            MemoryStream stdin = new MemoryStream();
            MemoryStream stdout = new MemoryStream();
            MemoryStream stderr = new MemoryStream();
            FakeHighResolutionClock clock = new FakeHighResolutionClock();
            FakeTimeProvider timeProvider = new FakeTimeProvider();
            FileSystemScriptLoader loader = new FileSystemScriptLoader();
            Func<string, string> debugInput = prompt => $"seen:{prompt}";
            Action<string> debugPrint = _ => { };

            ScriptOptions defaults = new ScriptOptions
            {
                DebugInput = debugInput,
                DebugPrint = debugPrint,
                UseLuaErrorLocations = true,
                ColonOperatorClrCallbackBehaviour = ColonOperatorBehaviour.TreatAsDotOnUserData,
                Stdin = stdin,
                Stdout = stdout,
                Stderr = stderr,
                TailCallOptimizationThreshold = 7,
                ScriptLoader = loader,
                CheckThreadAccess = true,
                CompatibilityVersion = LuaCompatibilityVersion.Lua54,
                HighResolutionClock = clock,
                TimeProvider = timeProvider,
            };

            ScriptOptions copy = new ScriptOptions(defaults);

            Assert.Multiple(() =>
            {
                Assert.That(copy.DebugInput, Is.SameAs(debugInput));
                Assert.That(copy.DebugPrint, Is.SameAs(debugPrint));
                Assert.That(copy.DebugInput("prompt"), Is.EqualTo("seen:prompt"));
                Assert.That(copy.UseLuaErrorLocations, Is.True);
                Assert.That(
                    copy.ColonOperatorClrCallbackBehaviour,
                    Is.EqualTo(ColonOperatorBehaviour.TreatAsDotOnUserData)
                );
                Assert.That(copy.Stdin, Is.SameAs(stdin));
                Assert.That(copy.Stdout, Is.SameAs(stdout));
                Assert.That(copy.Stderr, Is.SameAs(stderr));
                Assert.That(copy.TailCallOptimizationThreshold, Is.EqualTo(7));
                Assert.That(copy.ScriptLoader, Is.SameAs(loader));
                Assert.That(copy.CheckThreadAccess, Is.True);
                Assert.That(copy.CompatibilityVersion, Is.EqualTo(LuaCompatibilityVersion.Lua54));
                Assert.That(copy.HighResolutionClock, Is.SameAs(clock));
                Assert.That(copy.TimeProvider, Is.SameAs(timeProvider));
            });
        }

        [Test]
        public void CopyConstructorGuardsAgainstNullDefaults()
        {
            Assert.Throws<ArgumentNullException>(() => new ScriptOptions(defaults: null));
        }

        [Test]
        public void HighResolutionClockFlowsIntoPerformanceStatistics()
        {
            FakeHighResolutionClock clock = new();
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                HighResolutionClock = clock,
            };

            Script script = new(options);
            script.PerformanceStats.Enabled = true;

            using (script.PerformanceStats.StartStopwatch(PerformanceCounter.Execution))
            {
                clock.AdvanceMilliseconds(12);
            }

            PerformanceResult result = script.PerformanceStats.GetPerformanceCounterResult(
                PerformanceCounter.Execution
            );

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Counter, Is.EqualTo(12));
        }

        [Test]
        public void CallRecordsExecutionCounterWhenEnabled()
        {
            Script script = new();
            script.PerformanceStats.Enabled = true;

            script.DoString("function add(a, b) return a + b end");
            DynValue result = script.Call(
                script.Globals.Get("add"),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3)
            );

            Assert.That(result.Number, Is.EqualTo(5));

            PerformanceResult stats = script.PerformanceStats.GetPerformanceCounterResult(
                PerformanceCounter.Execution
            );

            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.Instances, Is.GreaterThanOrEqualTo(1));
        }
    }
}
