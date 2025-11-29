#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Diagnostics.PerformanceCounters;

    public sealed class PerformanceStopwatchTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task StartAndDisposeProducesResult()
        {
            using PerformanceStopwatch stopwatch = new(PerformanceCounter.Execution);

            using (stopwatch.Start())
            {
                await Task.Delay(1);
            }

            PerformanceResult result = stopwatch.GetResult();

            await Assert.That(result.Name).IsEqualTo(PerformanceCounter.Execution.ToString());
            await Assert.That(result.Type).IsEqualTo(PerformanceCounterType.TimeMilliseconds);
            await Assert.That(result.Instances).IsEqualTo(1);
            await Assert.That(result.Counter >= 0).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ReentrantStartCountsSingleInstance()
        {
            using PerformanceStopwatch stopwatch = new(PerformanceCounter.Compilation);

            using (stopwatch.Start())
            {
                using (stopwatch.Start())
                {
                    await Task.Delay(1);
                }
            }

            PerformanceResult result = stopwatch.GetResult();

            await Assert.That(result.Instances).IsEqualTo(1);
            await Assert.That(result.Counter >= 0).IsTrue();
        }
    }
}
#pragma warning restore CA2007
