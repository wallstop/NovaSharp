namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;

    /// <summary>
    /// Temporarily overrides <see cref="Processor.CanYield"/> and restores the previous value when disposed.
    /// </summary>
    internal sealed class ProcessorYieldScope : IDisposable
    {
        private readonly Processor _processor;
        private readonly bool _originalValue;
        private bool _disposed;

        private ProcessorYieldScope(Processor processor, bool newValue)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _originalValue = processor.SwapCanYieldForTests(newValue);
        }

        public static ProcessorYieldScope Override(Processor processor, bool newValue)
        {
            return new ProcessorYieldScope(processor, newValue);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _processor.SwapCanYieldForTests(_originalValue);
            _disposed = true;
        }
    }
}
