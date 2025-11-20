namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
    using NovaSharp.Interpreter;
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
                string resolvedPath = ManifestCompatibilityHelper.ResolveScriptPath(arguments);
                ScriptOptions options = new(context.Script.Options);
                bool manifestApplied = ManifestCompatibilityHelper.TryApplyManifestCompatibility(
                    resolvedPath,
                    options
                );

                if (!manifestApplied)
                {
                    context.Script.DoFile(arguments);
                    return;
                }

                Script script = new(CoreModules.PresetComplete, options);
                script.DoFile(resolvedPath);
            }
        }
    }
}
