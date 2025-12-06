namespace WallstopStudios.NovaSharp.Cli
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security;
    using WallstopStudios.NovaSharp.Cli.Commands;
    using WallstopStudios.NovaSharp.Cli.Commands.Implementations;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modding;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.REPL;
    using WallstopStudios.NovaSharp.Interpreter.Utilities;

    /// <summary>
    /// Entry point for the NovaSharp CLI REPL and command processor.
    /// </summary>
    internal sealed partial class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            CommandManager.Initialize();

            Script.DefaultOptions.ScriptLoader = new ReplInterpreterScriptLoader();

            Script script = new(CoreModules.PresetComplete)
            {
                Globals = { ["makestatic"] = (Func<string, DynValue>)(MakeStatic) },
            };

            if (CheckArgs(args, new ShellContext(script)))
            {
                return;
            }

            Banner(script);

            ReplInterpreter interpreter = new(script)
            {
                HandleDynamicExprs = true,
                HandleClassicExprsSyntax = true,
            };

            while (true)
            {
                InterpreterLoop(interpreter, new ShellContext(script));
            }
        }

        private static DynValue MakeStatic(string type)
        {
            Type tt = Type.GetType(type);
            if (tt == null)
            {
                Console.WriteLine(CliMessages.ProgramTypeNotFound(type));
            }
            else
            {
                return UserData.CreateStatic(tt);
            }

            return DynValue.Nil;
        }

        private static void InterpreterLoop(ReplInterpreter interpreter, ShellContext shellContext)
        {
            Console.Write(interpreter.ClassicPrompt + " ");

            string s = Console.ReadLine() ?? string.Empty;

            if (!interpreter.HasPendingCommand && s.Length > 0 && s[0] == '!')
            {
                ExecuteCommand(shellContext, s.Substring(1));
                return;
            }

            try
            {
                DynValue result = interpreter.Evaluate(s);

                if (result != null && result.Type != DataType.Void)
                {
                    Console.WriteLine(result);
                }
            }
            catch (InterpreterException ex)
            {
                Console.WriteLine(ex.DecoratedMessage ?? ex.Message);
            }
            catch (Exception ex) when (IsRecoverableInterpreterLoopException(ex))
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void Banner(Script script)
        {
            Console.WriteLine(Script.GetBanner("Console"));
            Console.WriteLine();
            LuaCompatibilityProfile profile = script.CompatibilityProfile;
            Console.WriteLine(CliMessages.ProgramActiveProfile(profile.GetFeatureSummary()));
            Console.WriteLine(CliMessages.ProgramCompatibilityHint);
            Console.WriteLine(CliMessages.ProgramCompatibilityUsage);
            Console.WriteLine();
            Console.WriteLine(CliMessages.ProgramWelcome);
            Console.WriteLine();
        }

        /// <summary>
        /// Parses command-line arguments and executes any requested non-interactive actions.
        /// </summary>
        /// <param name="args">Raw command-line arguments.</param>
        /// <param name="shellContext">Shell context used for executing command-line commands.</param>
        /// <returns><c>true</c> when the application should exit after processing the arguments.</returns>
        internal static bool CheckArgs(string[] args, ShellContext shellContext)
        {
            if (args.Length == 0)
            {
                return false;
            }

            if (TryRunExecuteChunks(args))
            {
                return true;
            }

            if (TryRunScriptArgument(args))
            {
                return true;
            }

            if (args[0] == "-H" || args[0] == "--help" || args[0] == "/?" || args[0] == "-?")
            {
                ShowCmdLineHelpBig();
            }
            else if (args[0] == "-X")
            {
                if (args.Length == 2)
                {
                    ExecuteCommand(shellContext, args[1]);
                }
                else
                {
                    Console.WriteLine(CliMessages.ProgramWrongSyntax);
                    ShowCmdLineHelp();
                }
            }
            else if (args[0] == "-W")
            {
                bool internals = false;
                string dumpfile = null;
                string destfile = null;
                string classname = null;
                string namespacename = null;
                bool useVb = false;
                bool fail = true;

                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] == "--internals")
                    {
                        internals = true;
                    }
                    else if (args[i] == "--vb")
                    {
                        useVb = true;
                    }
                    else if (args[i].StartsWith("--class:", StringComparison.Ordinal))
                    {
                        classname = args[i].Substring("--class:".Length);
                    }
                    else if (args[i].StartsWith("--namespace:", StringComparison.Ordinal))
                    {
                        namespacename = args[i].Substring("--namespace:".Length);
                    }
                    else if (dumpfile == null)
                    {
                        dumpfile = args[i];
                    }
                    else if (destfile == null)
                    {
                        destfile = args[i];
                        fail = false;
                    }
                    else
                    {
                        fail = true;
                    }
                }

                if (fail)
                {
                    Console.WriteLine(CliMessages.ProgramWrongSyntax);
                    ShowCmdLineHelp();
                }
                else
                {
                    HardwireCommand.Generate(
                        useVb ? "vb" : "cs",
                        dumpfile,
                        destfile,
                        internals,
                        classname,
                        namespacename
                    );
                }
            }

            return true;
        }

        private static bool TryRunExecuteChunks(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return false;
            }

            List<string> inlineChunks = new();
            int index = 0;

            while (index < args.Length)
            {
                string current = args[index];
                if (!IsExecuteOption(current))
                {
                    break;
                }

                if (index + 1 >= args.Length)
                {
                    Console.WriteLine(CliMessages.ProgramWrongSyntax);
                    ShowCmdLineHelp();
                    return true;
                }

                inlineChunks.Add(args[index + 1]);
                index += 2;
            }

            if (inlineChunks.Count == 0)
            {
                return false;
            }

            if (index < args.Length)
            {
                Console.WriteLine(CliMessages.ProgramWrongSyntax);
                ShowCmdLineHelp();
                return true;
            }

            Script script = new(CoreModules.PresetComplete);

            foreach (string chunk in inlineChunks)
            {
                script.DoString(chunk);
            }

            return true;
        }

        private static bool IsExecuteOption(string argument)
        {
            return string.Equals(argument, "-e", StringComparison.Ordinal)
                || string.Equals(argument, "--execute", StringComparison.Ordinal);
        }

        private static bool TryRunScriptArgument(string[] args)
        {
            if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]) || args[0][0] == '-')
            {
                return false;
            }

            string resolvedScriptPath = ResolveScriptPath(args[0]);
            ScriptOptions options = new(Script.DefaultOptions);
            ModManifestCompatibility.TryApplyFromScriptPath(
                resolvedScriptPath,
                options,
                Script.GlobalOptions.CompatibilityVersion,
                info => Console.WriteLine(CliMessages.ContextualCompatibilityInfo(info)),
                warning => Console.WriteLine(CliMessages.CompatibilityWarning(warning))
            );

            Script script = new(CoreModules.PresetComplete, options);
            Console.WriteLine(
                CliMessages.ProgramRunningScript(
                    resolvedScriptPath,
                    script.CompatibilityProfile.GetFeatureSummary()
                )
            );
            script.DoFile(resolvedScriptPath);
            return true;
        }

        private static string ResolveScriptPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            ReadOnlySpan<char> trimmed = path.AsSpan().TrimWhitespace();
            string candidate = trimmed.Length == path.Length ? path : new string(trimmed);
            candidate = candidate.NormalizeDirectorySeparators(Path.DirectorySeparatorChar);

            try
            {
                return Path.GetFullPath(candidate);
            }
            catch (ArgumentException)
            {
                return candidate;
            }
            catch (PathTooLongException)
            {
                return candidate;
            }
            catch (NotSupportedException)
            {
                return candidate;
            }
            catch (SecurityException)
            {
                return candidate;
            }
        }

        private static void ShowCmdLineHelpBig()
        {
            Console.WriteLine(CliMessages.ProgramUsageLong);
            Console.WriteLine();
            Console.WriteLine(CliMessages.ProgramUsageHelpSwitch);
            Console.WriteLine(CliMessages.ProgramUsageExecuteSwitch);
            Console.WriteLine(CliMessages.ProgramUsageHardwireSwitch);
            Console.WriteLine();
        }

        private static void ShowCmdLineHelp()
        {
            Console.WriteLine(CliMessages.ProgramUsageShort);
        }

        private static void ExecuteCommand(ShellContext shellContext, string cmdline)
        {
            CommandManager.Execute(shellContext, cmdline);

            if (shellContext.IsExitRequested)
            {
                Environment.Exit(shellContext.ExitCode);
            }
        }

        private static bool IsRecoverableInterpreterLoopException(Exception exception)
        {
            return exception is IOException
                || exception is UnauthorizedAccessException
                || exception is InvalidOperationException
                || exception is ArgumentException
                || exception is FormatException;
        }
    }
}
