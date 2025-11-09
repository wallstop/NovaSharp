namespace NovaSharp.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class CommandManager
    {
        private static readonly Dictionary<string, ICommand> Registry = new();

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
