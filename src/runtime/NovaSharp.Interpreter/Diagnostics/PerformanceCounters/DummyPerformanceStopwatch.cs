namespace NovaSharp.Interpreter.Diagnostics.PerformanceCounters
{
    using System;

    internal class DummyPerformanceStopwatch : IPerformanceStopwatch, IDisposable
    {
        public static DummyPerformanceStopwatch Instance = new();
        private readonly PerformanceResult _result;

        private DummyPerformanceStopwatch()
        {
            _result = new PerformanceResult()
            {
                Counter = 0,
                Global = true,
                Instances = 0,
                Name = "::dummy::",
                Type = PerformanceCounterType.TimeMilliseconds,
            };
        }

        public IDisposable Start()
        {
            return this;
        }

        public PerformanceResult GetResult()
        {
            return _result;
        }

        public void Dispose() { }
    }
}
