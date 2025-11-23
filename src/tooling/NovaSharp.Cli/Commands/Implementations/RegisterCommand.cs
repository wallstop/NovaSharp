namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
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
            Console.WriteLine(
                "register [type] - register a CLR type or prints a list of registered types"
            );
        }

        /// <inheritdoc />
        public void DisplayLongHelp()
        {
            Console.WriteLine(
                "register [type] - register a CLR type or prints a list of registered types. Use makestatic('type') to make a static instance."
            );
        }

        /// <inheritdoc />
        public void Execute(ShellContext context, string argument)
        {
            if (argument.Length > 0)
            {
                Type t = Type.GetType(argument);
                if (t == null)
                {
                    Console.WriteLine("Type {0} not found.", argument);
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
