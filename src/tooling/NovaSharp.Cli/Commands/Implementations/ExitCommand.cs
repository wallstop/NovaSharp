namespace NovaSharp.Commands.Implementations
{
    class ExitCommand : ICommand
    {
        public string Name
        {
            get { return "exit"; }
        }

        public void DisplayShortHelp()
        {
            Console.WriteLine("exit - Exits the interpreter");
        }

        public void DisplayLongHelp()
        {
            Console.WriteLine("exit - Exits the interpreter");
        }

        public void Execute(ShellContext context, string arguments)
        {
            Environment.Exit(0);
        }
    }
}
