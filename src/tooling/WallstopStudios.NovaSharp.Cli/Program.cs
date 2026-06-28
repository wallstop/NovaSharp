namespace WallstopStudios.NovaSharp.Cli
{
    using System;
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

            Script script = new(CoreModulePresets.Complete)
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
        /// Uses <see cref="CliArgumentRegistry"/> for centralized argument parsing.
        /// </summary>
        /// <param name="args">Raw command-line arguments.</param>
        /// <param name="shellContext">Shell context used for executing command-line commands.</param>
        /// <returns><c>true</c> when the application should exit after processing the arguments.</returns>
        internal static bool CheckArgs(string[] args, ShellContext shellContext)
        {
            CliParseResult parseResult = CliArgumentRegistry.Parse(args);

            if (!parseResult.Success)
            {
                Console.WriteLine(parseResult.ErrorMessage);
                ShowCmdLineHelp();
                return true;
            }

            switch (parseResult.Mode)
            {
                case CliExecutionMode.Repl:
                    // Apply version to script if specified
                    if (parseResult.LuaVersion.HasValue)
                    {
                        shellContext.Script.Options.CompatibilityVersion = parseResult
                            .LuaVersion
                            .Value;
                    }

                    return false;

                case CliExecutionMode.Help:
                    ShowCmdLineHelpBig();
                    return true;

                case CliExecutionMode.Script:
                    return ExecuteScriptMode(parseResult);

                case CliExecutionMode.Execute:
                    return ExecuteInlineMode(parseResult);

                case CliExecutionMode.Hardwire:
                    return ExecuteHardwireMode(parseResult);

                case CliExecutionMode.Command:
                    ExecuteCommand(shellContext, parseResult.ReplCommand);
                    return true;

                default:
                    return false;
            }
        }

        private static bool ExecuteScriptMode(CliParseResult parseResult)
        {
            string resolvedScriptPath = ResolveScriptPath(parseResult.ScriptPath);
            ScriptOptions options = new(Script.DefaultOptions);

            // Apply explicit version if provided; otherwise try to detect from mod.json
            if (parseResult.LuaVersion.HasValue)
            {
                options.CompatibilityVersion = parseResult.LuaVersion.Value;
            }
            else
            {
                ModManifestCompatibility.TryApplyFromScriptPath(
                    resolvedScriptPath,
                    options,
                    Script.GlobalOptions.CompatibilityVersion,
                    info => Console.WriteLine(CliMessages.ContextualCompatibilityInfo(info)),
                    warning => Console.WriteLine(CliMessages.CompatibilityWarning(warning))
                );
            }

            Script script = new(CoreModulePresets.Complete, options);
            Console.WriteLine(
                CliMessages.ProgramRunningScript(
                    resolvedScriptPath,
                    script.CompatibilityProfile.GetFeatureSummary()
                )
            );
            script.DoFile(resolvedScriptPath);
            return true;
        }

        private static bool ExecuteInlineMode(CliParseResult parseResult)
        {
            LuaCompatibilityVersion luaVersion =
                parseResult.LuaVersion ?? LuaCompatibilityVersion.Latest;

            ScriptOptions options = new(Script.DefaultOptions)
            {
                CompatibilityVersion = luaVersion,
            };
            Script script = new(CoreModulePresets.Complete, options);

            foreach (string chunk in parseResult.InlineChunks)
            {
                script.DoString(chunk);
            }

            return true;
        }

        private static bool ExecuteHardwireMode(CliParseResult parseResult)
        {
            HardwireArguments hwArgs = parseResult.HardwireArgs;
            HardwireCommand.Generate(
                hwArgs.UseVisualBasic ? "vb" : "cs",
                hwArgs.DumpFile,
                hwArgs.DestinationFile,
                hwArgs.AllowInternals,
                hwArgs.ClassName,
                hwArgs.NamespaceName
            );
            return true;
        }

        /// <summary>
        /// Tries to parse a Lua version string (e.g., "5.2", "5.4", "54", "Lua54") into a <see cref="LuaCompatibilityVersion"/>.
        /// Delegates to <see cref="CliArgumentRegistry.TryParseLuaVersion"/>.
        /// </summary>
        /// <param name="versionString">The version string to parse.</param>
        /// <param name="version">The parsed version, or <see cref="LuaCompatibilityVersion.Latest"/> if parsing fails.</param>
        /// <returns><c>true</c> if parsing succeeded.</returns>
        internal static bool TryParseLuaVersion(
            string versionString,
            out LuaCompatibilityVersion version
        )
        {
            return CliArgumentRegistry.TryParseLuaVersion(versionString, out version);
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
            Console.WriteLine(CliArgumentRegistry.GenerateHelpText());
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
