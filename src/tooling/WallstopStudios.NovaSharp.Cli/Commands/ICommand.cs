namespace WallstopStudios.NovaSharp.Cli.Commands
{
    using WallstopStudios.NovaSharp.Cli;

    /// <summary>
    /// Represents a REPL command exposed by the NovaSharp CLI shell.
    /// </summary>
    internal interface ICommand
    {
        /// <summary>
        /// Gets the command token that users type in the REPL (e.g., <c>!run</c>).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Writes a single-line help summary to the console.
        /// </summary>
        public void DisplayShortHelp();

        /// <summary>
        /// Writes the detailed help text for the command.
        /// </summary>
        public void DisplayLongHelp();

        /// <summary>
        /// Executes the command with the supplied shell context and argument text.
        /// </summary>
        /// <param name="context">Active shell context (script instance, streams, exit state).</param>
        /// <param name="argument">Argument string supplied by the user after the command token.</param>
        public void Execute(ShellContext context, string argument);
    }
}
