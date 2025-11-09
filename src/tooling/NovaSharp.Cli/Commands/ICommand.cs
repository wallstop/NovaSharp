namespace NovaSharp.Commands
{
    public interface ICommand
    {
        string Name { get; }
        void DisplayShortHelp();
        void DisplayLongHelp();
        void Execute(ShellContext context, string argument);
    }
}
