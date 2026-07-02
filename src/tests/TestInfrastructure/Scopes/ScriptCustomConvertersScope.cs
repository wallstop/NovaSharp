namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Interop;

    /// <summary>
    /// Applies <see cref="Script.GlobalOptions.CustomConverters"/> overrides inside an isolated
    /// global options scope.
    /// </summary>
    internal sealed class ScriptCustomConvertersScope : IDisposable
    {
        private readonly IDisposable _globalScope;
        private bool _disposed;

        private ScriptCustomConvertersScope(IDisposable globalScope)
        {
            _globalScope = globalScope ?? throw new ArgumentNullException(nameof(globalScope));
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

                if (clear)
                {
                    current.Clear();
                }

                configure?.Invoke(Script.GlobalOptions.CustomConverters);
                return new ScriptCustomConvertersScope(globalScope);
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
