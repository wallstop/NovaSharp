namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
    using NovaSharp.Cli;
    using NovaSharp.Interpreter.Compatibility;

    /// <summary>
    /// CLI command that lists available commands or shows detailed help for a specific command.
    /// </summary>
    internal class HelpCommand : ICommand
    {
        /// <inheritdoc />
        public string Name
        {
            get { return "help"; }
        }

        /// <inheritdoc />
        public void DisplayShortHelp()
        {
            Console.WriteLine(CliMessages.HelpCommandShortHelp);
        }

        /// <inheritdoc />
        public void DisplayLongHelp()
        {
            DisplayShortHelp();
        }

        /// <inheritdoc />
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
                    Console.WriteLine(CliMessages.HelpCommandCommandNotFound(arguments));
                }
            }
            else
            {
                Console.WriteLine(CliMessages.HelpCommandPrimaryInstruction);
                Console.WriteLine(CliMessages.HelpCommandSecondaryInstruction);
                Console.WriteLine();
                Console.WriteLine(CliMessages.HelpCommandCommandListHeading);
                Console.WriteLine();

                foreach (ICommand cmd in CommandManager.GetCommands())
                {
                    Console.Write(CliMessages.HelpCommandCommandPrefix);
                    cmd.DisplayShortHelp();
                }

                Console.WriteLine();
                WriteCompatibilitySummary(context);
                Console.WriteLine();
            }
        }

        private static void WriteCompatibilitySummary(ShellContext context)
        {
            if (context?.Script == null)
            {
                return;
            }

            LuaCompatibilityProfile profile = context.Script.CompatibilityProfile;
            Console.WriteLine(CliMessages.ProgramActiveProfile(profile.GetFeatureSummary()));
            Console.WriteLine(CliMessages.HelpCommandCompatibilitySummary);
        }
    }
}
