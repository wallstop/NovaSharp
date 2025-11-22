namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
    using System.IO;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modding;
    using NovaSharp.Interpreter.Modules;

    internal sealed class RunCommand : ICommand
    {
        public string Name
        {
            get { return "run"; }
        }

        public void DisplayShortHelp()
        {
            Console.WriteLine("run <filename> - Executes the specified Lua script");
        }

        public void DisplayLongHelp()
        {
            Console.WriteLine("run <filename> - Executes the specified Lua script.");
        }

        public void Execute(ShellContext context, string arguments)
        {
            if (arguments.Length == 0)
            {
                Console.WriteLine("Syntax : !run <file>");
            }
            else
            {
                string resolvedPath = ResolveScriptPath(arguments);
                ScriptOptions options = new(context.Script.Options);
                bool manifestApplied = ModManifestCompatibility.TryApplyFromScriptPath(
                    resolvedPath,
                    options,
                    Script.GlobalOptions.CompatibilityVersion,
                    info => Console.WriteLine($"[compatibility] {info}"),
                    warning => Console.WriteLine($"[compatibility] {warning}")
                );

                if (!manifestApplied)
                {
                    LogCompatibilitySummary(resolvedPath, context.Script);
                    context.Script.DoFile(arguments);
                    return;
                }

                Script script = new(CoreModules.PresetComplete, options);
                LogCompatibilitySummary(resolvedPath, script);
                script.DoFile(resolvedPath);
            }
        }

        private static string ResolveScriptPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception)
            {
                return path;
            }
        }

        private static void LogCompatibilitySummary(string scriptPath, Script script)
        {
            LuaCompatibilityProfile profile = script.CompatibilityProfile;
            Console.WriteLine(
                $"[compatibility] Running '{scriptPath}' with {profile.GetFeatureSummary()}"
            );
        }
    }
}
