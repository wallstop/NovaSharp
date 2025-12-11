namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;

    /// <summary>
    /// Captures <see cref="Script.DefaultOptions"/> and restores them on disposal.
    /// </summary>
    internal sealed class ScriptDefaultOptionsScope : IDisposable
    {
        private readonly IDisposable _scope;

        private ScriptDefaultOptionsScope(IDisposable scope, Action<ScriptOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(scope);
            _scope = scope;
            configure?.Invoke(Script.DefaultOptions);
        }

        public static ScriptDefaultOptionsScope Enter(Action<ScriptOptions> configure = null)
        {
            return new ScriptDefaultOptionsScope(Script.BeginDefaultOptionsScope(), configure);
        }

        public static ScriptDefaultOptionsScope OverrideScriptLoader(IScriptLoader scriptLoader)
        {
            ArgumentNullException.ThrowIfNull(scriptLoader);
            return Enter(options => options.ScriptLoader = scriptLoader);
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
