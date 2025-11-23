namespace NovaSharp.Cli.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NovaSharp.Cli.Commands.Implementations;

    /// <summary>
    /// Discovers CLI commands via reflection and routes REPL invocations to the appropriate handlers.
    /// </summary>
    public static class CommandManager
    {
        /// <summary>
        /// Backing store mapping command names to their handlers.
        /// </summary>
        private static readonly Dictionary<string, ICommand> Registry = new();

        /// <summary>
        /// Scans the current assembly for <see cref="ICommand"/> implementations and populates the registry.
        /// </summary>
        public static void Initialize()
        {
            foreach (
                Type t in Assembly
                    .GetExecutingAssembly()
                    .GetTypes()
                    .Where(tt => typeof(ICommand).IsAssignableFrom(tt))
                    .Where(tt => tt.IsClass && (!tt.IsAbstract))
            )
            {
                object o = Activator.CreateInstance(t);
                ICommand cmd = (ICommand)o;
                Registry.Add(cmd.Name, cmd);
            }
        }

        /// <summary>
        /// Parses the REPL input line and executes the matching command.
        /// </summary>
        /// <param name="context">Active REPL context.</param>
        /// <param name="commandLine">Full command line entered by the user.</param>
        public static void Execute(ShellContext context, string commandLine)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (commandLine == null)
            {
                throw new ArgumentNullException(nameof(commandLine));
            }

            string trimmed = commandLine.Trim();

            if (trimmed.Length == 0)
            {
                Console.WriteLine("Invalid command ''.");
                return;
            }

            int separatorIndex = 0;

            while (separatorIndex < trimmed.Length && !char.IsWhiteSpace(trimmed[separatorIndex]))
            {
                separatorIndex++;
            }

            string command = trimmed.Substring(0, separatorIndex);
            string arguments =
                separatorIndex >= trimmed.Length
                    ? string.Empty
                    : trimmed.Substring(separatorIndex).Trim();

            ICommand cmd = Find(command);

            if (cmd == null)
            {
                Console.WriteLine("Invalid command '{0}'.", command);
                return;
            }

            cmd.Execute(context, arguments);
        }

        /// <summary>
        /// Returns the registered commands ordered as expected by <c>!help</c> (help command first).
        /// </summary>
        public static IEnumerable<ICommand> GetCommands()
        {
            yield return Registry["help"];

            foreach (
                ICommand cmd in Registry.Values.Where(c => !(c is HelpCommand)).OrderBy(c => c.Name)
            )
            {
                yield return cmd;
            }
        }

        /// <summary>
        /// Looks up a command implementation by its name.
        /// </summary>
        /// <param name="cmd">Command token (e.g., <c>help</c>).</param>
        /// <returns>The registered command or <c>null</c> when no match exists.</returns>
        public static ICommand Find(string cmd)
        {
            if (Registry.ContainsKey(cmd))
            {
                return Registry[cmd];
            }

            return null;
        }
    }
}
