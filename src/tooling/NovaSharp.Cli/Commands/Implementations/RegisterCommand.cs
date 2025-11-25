namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
    using NovaSharp.Cli;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// CLI command that registers CLR types for userdata exposure or lists the registered types.
    /// </summary>
    internal class RegisterCommand : ICommand
    {
        /// <inheritdoc />
        public string Name
        {
            get { return "register"; }
        }

        /// <inheritdoc />
        public void DisplayShortHelp()
        {
            Console.WriteLine(CliMessages.RegisterCommandShortHelp);
        }

        /// <inheritdoc />
        public void DisplayLongHelp()
        {
            Console.WriteLine(CliMessages.RegisterCommandLongHelp);
        }

        /// <inheritdoc />
        public void Execute(ShellContext context, string argument)
        {
            if (argument.Length > 0)
            {
                Type t = Type.GetType(argument);
                if (t == null)
                {
                    Console.WriteLine(CliMessages.RegisterCommandTypeNotFound(argument));
                }
                else
                {
                    UserData.RegisterType(t);
                }
            }
            else
            {
                foreach (Type type in UserData.GetRegisteredTypes())
                {
                    Console.WriteLine(type.FullName);
                }
            }
        }
    }
}
