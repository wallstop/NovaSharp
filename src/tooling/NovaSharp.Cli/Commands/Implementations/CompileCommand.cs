namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
    using System.IO;
    using NovaSharp.Cli;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// CLI command that compiles a Lua file into a NovaSharp binary chunk.
    /// </summary>
    internal sealed class CompileCommand : ICommand
    {
        /// <inheritdoc />
        public string Name
        {
            get { return "compile"; }
        }

        /// <inheritdoc />
        public void DisplayShortHelp()
        {
            Console.WriteLine(CliMessages.CompileCommandShortHelp);
        }

        /// <inheritdoc />
        public void DisplayLongHelp()
        {
            Console.WriteLine(CliMessages.CompileCommandLongHelp);
        }

        /// <inheritdoc />
        public void Execute(ShellContext context, string p)
        {
            string targetFileName = p + "-compiled";

            Script s = new Script(CoreModules.PresetDefault);

            DynValue chunk = s.LoadFile(p);

            using Stream stream = new FileStream(targetFileName, FileMode.Create, FileAccess.Write);
            s.Dump(chunk, stream);
        }
    }
}
