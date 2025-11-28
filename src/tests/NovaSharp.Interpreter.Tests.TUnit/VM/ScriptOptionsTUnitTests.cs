namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Infrastructure;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Options;
    using NovaSharp.Interpreter.Tests.TestUtilities;

    public sealed class ScriptOptionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DefaultConstructorSeedsBaselineServices()
        {
            ScriptOptions options = new ScriptOptions();

            await Assert
                .That(options.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Latest);
            await Assert
                .That(options.HighResolutionClock)
                .IsSameReferenceAs(SystemHighResolutionClock.Instance);
            await Assert.That(options.TimeProvider).IsSameReferenceAs(SystemTimeProvider.Instance);
        }

        [global::TUnit.Core.Test]
        public async Task CopyConstructorClonesConfiguredValues()
        {
            MemoryStream stdin = new();
            MemoryStream stdout = new();
            MemoryStream stderr = new();
            FakeHighResolutionClock clock = new();
            FakeTimeProvider timeProvider = new();
            FileSystemScriptLoader loader = new();
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

            await Assert.That(copy.DebugInput).IsSameReferenceAs(debugInput);
            await Assert.That(copy.DebugPrint).IsSameReferenceAs(debugPrint);
            await Assert.That(copy.DebugInput!("prompt")).IsEqualTo("seen:prompt");
            await Assert.That(copy.UseLuaErrorLocations).IsTrue();
            await Assert
                .That(copy.ColonOperatorClrCallbackBehaviour)
                .IsEqualTo(ColonOperatorBehaviour.TreatAsDotOnUserData);
            await Assert.That(copy.Stdin).IsSameReferenceAs(stdin);
            await Assert.That(copy.Stdout).IsSameReferenceAs(stdout);
            await Assert.That(copy.Stderr).IsSameReferenceAs(stderr);
            await Assert.That(copy.TailCallOptimizationThreshold).IsEqualTo(7);
            await Assert.That(copy.ScriptLoader).IsSameReferenceAs(loader);
            await Assert.That(copy.CheckThreadAccess).IsTrue();
            await Assert.That(copy.CompatibilityVersion).IsEqualTo(LuaCompatibilityVersion.Lua54);
            await Assert.That(copy.HighResolutionClock).IsSameReferenceAs(clock);
            await Assert.That(copy.TimeProvider).IsSameReferenceAs(timeProvider);
        }

        [global::TUnit.Core.Test]
        public async Task CopyConstructorGuardsAgainstNullDefaults()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                new ScriptOptions(defaults: null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("defaults");
        }

        [global::TUnit.Core.Test]
        public async Task HighResolutionClockFlowsIntoPerformanceStatistics()
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

            await Assert.That(result).IsNotNull();
            await Assert.That(result.Counter).IsEqualTo(12);
        }

        [global::TUnit.Core.Test]
        public async Task CallRecordsExecutionCounterWhenEnabled()
        {
            Script script = new();
            script.PerformanceStats.Enabled = true;

            script.DoString("function add(a, b) return a + b end");
            DynValue result = script.Call(
                script.Globals.Get("add"),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3)
            );

            await Assert.That(result.Number).IsEqualTo(5);

            PerformanceResult stats = script.PerformanceStats.GetPerformanceCounterResult(
                PerformanceCounter.Execution
            );

            await Assert.That(stats).IsNotNull();
            await Assert.That(stats.Instances).IsGreaterThanOrEqualTo(1);
        }

        private static TException ExpectException<TException>(Func<ScriptOptions> factory)
            where TException : Exception
        {
            try
            {
                factory();
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
}
