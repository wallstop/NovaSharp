namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

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
