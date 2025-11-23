namespace NovaSharp.Cli.Commands.Implementations
{
    using System;

    /// <summary>
    /// CLI command that terminates the REPL loop.
    /// </summary>
    internal sealed class ExitCommand : ICommand
    {
        /// <inheritdoc />
        public string Name
        {
            get { return "exit"; }
        }

        /// <inheritdoc />
        public void DisplayShortHelp()
        {
            Console.WriteLine("exit - Exits the interpreter");
        }

        /// <inheritdoc />
        public void DisplayLongHelp()
        {
            Console.WriteLine("exit - Exits the interpreter");
        }

        /// <inheritdoc />
        public void Execute(ShellContext context, string arguments)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.RequestExit();
        }
    }
}
