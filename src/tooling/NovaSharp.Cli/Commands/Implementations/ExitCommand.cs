namespace NovaSharp.Commands.Implementations
{
    using System;

    internal sealed class ExitCommand : ICommand
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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.RequestExit();
        }
    }
}
