namespace NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Interop;

    /// <summary>
    /// Captures <see cref="Script.GlobalOptions.CustomConverters"/> and restores the original mappings on disposal.
    /// </summary>
    internal sealed class ScriptCustomConvertersScope : IDisposable
    {
        private readonly CustomConverterRegistry _snapshot;
        private bool _disposed;

        private ScriptCustomConvertersScope(CustomConverterRegistry snapshot)
        {
            _snapshot = snapshot ?? new CustomConverterRegistry();
        }

        public static ScriptCustomConvertersScope Capture(
            bool clear = false,
            Action<CustomConverterRegistry> configure = null
        )
        {
            CustomConverterRegistry current = EnsureRegistry();
            CustomConverterRegistry snapshot = current.Clone();

            if (clear)
            {
                current.Clear();
            }

            configure?.Invoke(Script.GlobalOptions.CustomConverters);
            return new ScriptCustomConvertersScope(snapshot);
        }

        public static ScriptCustomConvertersScope Clear(
            Action<CustomConverterRegistry> configure = null
        )
        {
            return Capture(clear: true, configure);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Script.GlobalOptions.CustomConverters =
                _snapshot?.Clone() ?? new CustomConverterRegistry();
            _disposed = true;
        }

        private static CustomConverterRegistry EnsureRegistry()
        {
            if (Script.GlobalOptions.CustomConverters == null)
            {
                Script.GlobalOptions.CustomConverters = new CustomConverterRegistry();
            }

            return Script.GlobalOptions.CustomConverters;
        }
    }
}
