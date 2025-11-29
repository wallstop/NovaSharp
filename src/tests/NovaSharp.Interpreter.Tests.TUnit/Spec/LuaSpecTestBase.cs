namespace NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Shared helpers for spec-driven fixtures that execute against multiple Lua compatibility versions.
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

