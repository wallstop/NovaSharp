namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
    using System.IO;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;

    internal sealed class CompileCommand : ICommand
    {
        public string Name
        {
            get { return "compile"; }
        }

        public void DisplayShortHelp()
        {
            Console.WriteLine("compile <filename> - Compiles the file in a binary format");
        }

        public void DisplayLongHelp()
        {
            Console.WriteLine(
                "compile <filename> - Compiles the file in a binary format.\nThe destination filename will be appended with '-compiled'."
            );
        }

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
