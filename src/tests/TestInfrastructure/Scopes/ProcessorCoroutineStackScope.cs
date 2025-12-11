namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;

    /// <summary>
    /// Captures a processor's coroutine stack and restores it on disposal.
    /// </summary>
    internal sealed class ProcessorCoroutineStackScope : IDisposable
    {
        private readonly Processor _processor;
        private readonly List<Processor> _snapshot;
        private bool _disposed;

        private ProcessorCoroutineStackScope(Processor processor)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _snapshot = new List<Processor>(processor.GetCoroutineStackForTests());
        }

        public static ProcessorCoroutineStackScope Capture(Processor processor)
        {
            return new ProcessorCoroutineStackScope(processor);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _processor.ReplaceCoroutineStackForTests(_snapshot);
            _disposed = true;
        }
    }
}
