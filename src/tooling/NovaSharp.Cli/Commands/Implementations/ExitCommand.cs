namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
    using NovaSharp.Cli;

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
            Console.WriteLine(CliMessages.ExitCommandShortHelp);
        }

        /// <inheritdoc />
        public void DisplayLongHelp()
        {
            Console.WriteLine(CliMessages.ExitCommandLongHelp);
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
