namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Options;

    /// <summary>
    /// Applies overrides to <see cref="Script.GlobalOptions"/> and restores the previous snapshot on disposal.
    /// </summary>
    internal sealed class ScriptGlobalOptionsScope : IDisposable
    {
        private readonly IDisposable _scope;
        private bool _disposed;

        private ScriptGlobalOptionsScope(Action<ScriptGlobalOptions> configure)
        {
            _scope = Script.BeginGlobalOptionsScope();
            configure?.Invoke(Script.GlobalOptions);
        }

        public static ScriptGlobalOptionsScope Override(Action<ScriptGlobalOptions> configure)
        {
            return new ScriptGlobalOptionsScope(configure);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _scope.Dispose();
        }
    }
}
