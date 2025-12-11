namespace WallstopStudios.NovaSharp.Cli.Commands.Implementations
{
    using System;
    using System.IO;
    using WallstopStudios.NovaSharp.Cli;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Utilities;

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
        public void Execute(ShellContext context, string argument)
        {
            ReadOnlySpan<char> trimmedArgument = (argument ?? string.Empty)
                .AsSpan()
                .TrimWhitespace();

            if (trimmedArgument.IsEmpty)
            {
                Console.WriteLine(CliMessages.ProgramWrongSyntax);
                return;
            }

            string sourcePath =
                trimmedArgument.Length == (argument?.Length ?? 0)
                    ? argument
                    : new string(trimmedArgument);
            string targetFileName = sourcePath + "-compiled";

            Script s = new Script(CoreModulePresets.Default);
            try
            {
                DynValue chunk = s.LoadFile(sourcePath);

                using Stream stream = new FileStream(
                    targetFileName,
                    FileMode.Create,
                    FileAccess.Write
                );
                s.Dump(chunk, stream);
                Console.WriteLine(CliMessages.CompileCommandSuccess(targetFileName));
            }
            catch (Exception ex) when (IsRecoverableCompileException(ex))
            {
                Console.WriteLine(CliMessages.CompileCommandFailure(sourcePath, ex.Message));
            }
        }

        private static bool IsRecoverableCompileException(Exception ex)
        {
            return ex switch
            {
                IOException => true,
                UnauthorizedAccessException => true,
                ArgumentException => true,
                NotSupportedException => true,
                SyntaxErrorException => true,
                ScriptRuntimeException => true,
                _ => false,
            };
        }
    }
}
