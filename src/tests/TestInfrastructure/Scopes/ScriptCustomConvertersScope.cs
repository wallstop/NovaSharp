namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Interop;

    /// <summary>
    /// Captures <see cref="Script.GlobalOptions.CustomConverters"/> in an isolated global options
    /// scope and restores the original mappings on disposal.
    /// </summary>
    internal sealed class ScriptCustomConvertersScope : IDisposable
    {
        private readonly IDisposable _globalScope;
        private readonly CustomConverterRegistry _snapshot;
        private bool _disposed;

        private ScriptCustomConvertersScope(
            IDisposable globalScope,
            CustomConverterRegistry snapshot
        )
        {
            _globalScope = globalScope ?? throw new ArgumentNullException(nameof(globalScope));
            _snapshot = snapshot ?? new CustomConverterRegistry();
        }

        public static ScriptCustomConvertersScope Capture(
            bool clear = false,
            Action<CustomConverterRegistry> configure = null
        )
        {
            IDisposable globalScope = Script.BeginGlobalOptionsScope();
            try
            {
                CustomConverterRegistry current = EnsureRegistry();
                CustomConverterRegistry snapshot = current.Clone();

                if (clear)
                {
                    current.Clear();
                }

                configure?.Invoke(Script.GlobalOptions.CustomConverters);
                return new ScriptCustomConvertersScope(globalScope, snapshot);
            }
            catch
            {
                globalScope.Dispose();
                throw;
            }
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
            _globalScope.Dispose();
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
