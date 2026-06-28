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
        /// <summary>
        /// Creates a script with the specified Lua version and core modules.
        /// </summary>
        /// <param name="compatibilityVersion">The Lua version to target.</param>
        /// <param name="modules">The core modules to register.</param>
        /// <returns>A configured <see cref="Script"/> instance.</returns>
        protected static Script CreateScript(
            LuaCompatibilityVersion compatibilityVersion,
            CoreModules modules
        )
        {
            return new Script(compatibilityVersion, modules);
        }

        /// <summary>
        /// Creates a script with the specified Lua version and default modules.
        /// </summary>
        /// <param name="compatibilityVersion">The Lua version to target.</param>
        /// <returns>A configured <see cref="Script"/> instance.</returns>
        protected static Script CreateScript(LuaCompatibilityVersion compatibilityVersion)
        {
            return new Script(compatibilityVersion);
        }
    }
}
