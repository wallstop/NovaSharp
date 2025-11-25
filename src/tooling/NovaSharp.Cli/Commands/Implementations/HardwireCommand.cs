namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
    using System.IO;
    using Hardwire;
    using Hardwire.Languages;
    using NovaSharp.Cli;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// CLI command that guides users through generating hardwired userdata descriptors from a Lua dump.
    /// </summary>
    internal sealed class HardwireCommand : ICommand
    {
        /// <summary>
        /// Logger implementation that funnels hardwire generation messages to the console.
        /// </summary>
        private class ConsoleLogger : ICodeGenerationLogger
        {
            /// <summary>
            /// Gets the number of errors emitted during generation.
            /// </summary>
            public int ErrorCount { get; private set; }

            /// <summary>
            /// Gets the number of warnings emitted during generation.
            /// </summary>
            public int WarningCount { get; private set; }

            /// <inheritdoc />
            public void LogError(string message)
            {
                Console.WriteLine(CliMessages.HardwireErrorLog(message));
                ErrorCount++;
            }

            /// <inheritdoc />
            public void LogWarning(string message)
            {
                Console.WriteLine(CliMessages.HardwireWarningLog(message));
                WarningCount++;
            }

            /// <inheritdoc />
            public void LogMinor(string message)
            {
                Console.WriteLine(CliMessages.HardwireInfoLog(message));
            }
        }

        /// <inheritdoc />
        public string Name
        {
            get { return "hardwire"; }
        }

        /// <inheritdoc />
        public void DisplayShortHelp()
        {
            Console.WriteLine(CliMessages.HardwireCommandShortHelp);
        }

        /// <inheritdoc />
        public void DisplayLongHelp()
        {
            Console.WriteLine(CliMessages.HardwireCommandLongHelp);
            Console.WriteLine();
        }

        /// <inheritdoc />
        public void Execute(ShellContext context, string argument)
        {
            Console.WriteLine(CliMessages.HardwireCommandAbortHint);
            Console.WriteLine();

            string language = AskQuestion(
                CliMessages.HardwireLanguagePrompt,
                "cs",
                s => s == "cs" || s == "vb",
                CliMessages.HardwireLanguageValidation
            );

            if (language == null)
            {
                return;
            }

            string luafile = AskQuestion(
                CliMessages.HardwireDumpPrompt,
                "",
                s => File.Exists(s),
                CliMessages.HardwireMissingFile
            );

            if (luafile == null)
            {
                return;
            }

            string destfile = AskQuestion(
                CliMessages.HardwireDestinationPrompt,
                "",
                s => true,
                string.Empty
            );

            if (destfile == null)
            {
                return;
            }

            string allowinternals = AskQuestion(
                CliMessages.HardwireInternalsPrompt,
                "y",
                s => s == "y" || s == "n",
                string.Empty
            );

            if (allowinternals == null)
            {
                return;
            }

            string namespaceName = AskQuestion(
                CliMessages.HardwireNamespacePrompt,
                "HardwiredClasses",
                s => IsValidIdentifier(s),
                CliMessages.HardwireIdentifierValidation
            );

            if (namespaceName == null)
            {
                return;
            }

            string className = AskQuestion(
                CliMessages.HardwireClassPrompt,
                "HardwireTypes",
                s => IsValidIdentifier(s),
                CliMessages.HardwireIdentifierValidation
            );

            if (className == null)
            {
                return;
            }

            Generate(language, luafile, destfile, allowinternals == "y", className, namespaceName);
        }

        private static bool IsValidIdentifier(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            foreach (char c in s)
            {
                if (c != '_' && !char.IsLetterOrDigit(c))
                {
                    return false;
                }
            }

            if (char.IsDigit(s[0]))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Generates hardwire descriptors by loading the provided Lua dump and emitting source code.
        /// </summary>
        /// <param name="language">Target language (`cs` or `vb`).</param>
        /// <param name="luafile">Path to the Lua dump table.</param>
        /// <param name="destfile">Destination path for the generated source code.</param>
        /// <param name="allowInternals">Whether internals should be exposed via the descriptors.</param>
        /// <param name="classname">Name of the generated class.</param>
        /// <param name="namespacename">Namespace that will contain the generated class.</param>
        public static void Generate(
            string language,
            string luafile,
            string destfile,
            bool allowInternals,
            string classname,
            string namespacename
        )
        {
            ConsoleLogger logger = new();
            try
            {
                Table t = DumpLoader(luafile);

                HardwireGeneratorRegistry.RegisterPredefined();

                HardwireGenerator hcg = new(
                    namespacename ?? "HardwiredClasses",
                    classname ?? "HardwireTypes",
                    logger,
                    language == "vb"
                        ? HardwireCodeGenerationLanguage.Vb
                        : HardwireCodeGenerationLanguage.CSharp
                )
                {
                    AllowInternals = allowInternals,
                };

                hcg.BuildCodeModel(t);

                string code = hcg.GenerateSourceCode();

                File.WriteAllText(destfile, code);
            }
            catch (InterpreterException ex)
            {
                Console.WriteLine(
                    CliMessages.HardwireInternalError(ex.DecoratedMessage ?? ex.Message)
                );
            }
            catch (IOException ex)
            {
                Console.WriteLine(CliMessages.HardwireInternalError(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine(CliMessages.HardwireInternalError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(CliMessages.HardwireInternalError(ex.Message));
            }

            Console.WriteLine();
            Console.WriteLine(
                CliMessages.HardwireGenerationSummary(logger.ErrorCount, logger.WarningCount)
            );
        }

        /// <summary>
        /// Loader function used to read hardwire dump tables; overridable for tests.
        /// </summary>
        internal static Func<string, Table> DumpLoader { get; set; } = LoadDumpTable;

        private static Table LoadDumpTable(string path)
        {
            Script s = new Script(CoreModules.None);
            DynamicExpression eee = s.CreateDynamicExpression(File.ReadAllText(path));
            return eee.Evaluate(null).Table;
        }

        private static string AskQuestion(
            string prompt,
            string defval,
            Func<string, bool> validator,
            string errormsg
        )
        {
            while (true)
            {
                Console.Write(prompt);
                string inp = Console.ReadLine();

                if (inp == "#quit")
                {
                    return null;
                }

                if (inp == "")
                {
                    inp = defval;
                }

                if (validator(inp))
                {
                    return inp;
                }

                Console.WriteLine(errormsg);
            }
        }
    }
}
