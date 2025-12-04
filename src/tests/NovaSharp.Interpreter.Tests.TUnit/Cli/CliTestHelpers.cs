namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using NovaSharp.Cli;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modules;

    internal static class CliTestHelpers
    {
        public static ShellContext CreateShellContext(Script script = null)
        {
            return new ShellContext(script ?? new Script(CoreModules.PresetComplete));
        }

        public static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }

        public static Script CreateScript(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new() { CompatibilityVersion = version };
            return new Script(CoreModules.PresetComplete, options);
        }
    }
}
