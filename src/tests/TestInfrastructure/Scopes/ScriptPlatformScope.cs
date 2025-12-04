namespace NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Platforms;

    /// <summary>
    /// Temporarily overrides <see cref="Script.GlobalOptions.Platform"/> and restores the previous accessor when disposed.
    /// </summary>
    internal sealed class ScriptPlatformScope : IDisposable
    {
        private readonly IDisposable _globalScope;
        private readonly IPlatformAccessor _previousPlatform;
        private bool _disposed;

        private ScriptPlatformScope(IPlatformAccessor replacement)
        {
            ArgumentNullException.ThrowIfNull(replacement);
            _globalScope = Script.BeginGlobalOptionsScope();
            _previousPlatform = Script.GlobalOptions.Platform;
            Script.GlobalOptions.Platform = replacement;
        }

        public static ScriptPlatformScope Override(IPlatformAccessor replacement)
        {
            return new ScriptPlatformScope(replacement);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Script.GlobalOptions.Platform = _previousPlatform;
            _globalScope.Dispose();
        }
    }
}
