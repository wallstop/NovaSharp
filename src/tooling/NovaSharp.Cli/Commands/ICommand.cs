namespace NovaSharp.Cli.Commands
{
    using NovaSharp.Cli;

    public interface ICommand
    {
        public string Name { get; }
        public void DisplayShortHelp();
        public void DisplayLongHelp();
        public void Execute(ShellContext context, string argument);
    }
}
