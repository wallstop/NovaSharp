namespace NovaSharp.Interpreter.Tests.Spec
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Shared helpers for spec-driven test fixtures that need to run across multiple compatibility versions.
    /// </summary>
    public abstract class LuaSpecTestBase
    {
        protected static Script CreateScript(
            LuaCompatibilityVersion compatibilityVersion,
            CoreModules modules
        )
        {
            ScriptOptions options = new(Script.DefaultOptions)
            {
                CompatibilityVersion = compatibilityVersion,
            };

            return new Script(modules, options);
        }
    }
}
