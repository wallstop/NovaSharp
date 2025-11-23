namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
    using NovaSharp.Cli;
    using NovaSharp.Interpreter.Compatibility;

    internal class HelpCommand : ICommand
    {
        public string Name
        {
            get { return "help"; }
        }

        public void DisplayShortHelp()
        {
            Console.WriteLine(
                "help [command] - gets the list of possible commands or help about the specified command"
            );
        }

        public void DisplayLongHelp()
        {
            DisplayShortHelp();
        }

        public void Execute(ShellContext context, string arguments)
        {
            string command = arguments ?? string.Empty;
            if (command.Length > 0)
            {
                ICommand cmd = CommandManager.Find(command);
                if (cmd != null)
                {
                    cmd.DisplayLongHelp();
                }
                else
                {
                    Console.WriteLine("Command '{0}' not found.", arguments);
                }
            }
            else
            {
                Console.WriteLine("Type Lua code to execute Lua code (multilines are accepted)");
                Console.WriteLine("or type one of the following commands to execute them.");
                Console.WriteLine("");
                Console.WriteLine("Commands:");
                Console.WriteLine("");

                foreach (ICommand cmd in CommandManager.GetCommands())
                {
                    Console.Write("  !");
                    cmd.DisplayShortHelp();
                }

                Console.WriteLine("");
                WriteCompatibilitySummary(context);
                Console.WriteLine("");
            }
        }

        private static void WriteCompatibilitySummary(ShellContext context)
        {
            if (context?.Script == null)
            {
                return;
            }

            LuaCompatibilityProfile profile = context.Script.CompatibilityProfile;
            Console.WriteLine($"Active compatibility profile: {profile.GetFeatureSummary()}");
            Console.WriteLine(
                "Use Script.Options.CompatibilityVersion or set luaCompatibility in mod.json to change it."
            );
        }
    }
}
