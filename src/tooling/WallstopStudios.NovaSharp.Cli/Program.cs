namespace WallstopStudios.NovaSharp.Cli
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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

        /// <summary>
        /// Tries to parse a Lua version string (e.g., "5.2", "5.4", "54", "Lua54") into a <see cref="LuaCompatibilityVersion"/>.
        /// </summary>
        /// <param name="versionString">The version string to parse.</param>
        /// <param name="version">The parsed version, or <see cref="LuaCompatibilityVersion.Latest"/> if parsing fails.</param>
        /// <returns><c>true</c> if parsing succeeded.</returns>
        internal static bool TryParseLuaVersion(
            string versionString,
            out LuaCompatibilityVersion version
        )
        {
            version = LuaCompatibilityVersion.Latest;

            if (string.IsNullOrWhiteSpace(versionString))
            {
                return false;
            }

            // Normalize: remove "lua" prefix if present, trim whitespace
            string normalized = versionString.Trim();
            if (
                normalized.StartsWith("lua", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("Lua", StringComparison.Ordinal)
            )
            {
                normalized = normalized.Substring(3);
            }

            // Remove dots: "5.4" -> "54"
            normalized = normalized.Replace(".", string.Empty, StringComparison.Ordinal);

            // Try parse as integer
            if (
                int.TryParse(
                    normalized,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int numericVersion
                )
            )
            {
                switch (numericVersion)
                {
                    case 51:
                        version = LuaCompatibilityVersion.Lua51;
                        return true;
                    case 52:
                        version = LuaCompatibilityVersion.Lua52;
                        return true;
                    case 53:
                        version = LuaCompatibilityVersion.Lua53;
                        return true;
                    case 54:
                        version = LuaCompatibilityVersion.Lua54;
                        return true;
                    case 55:
                        version = LuaCompatibilityVersion.Lua55;
                        return true;
                }
            }

            // Try parse "latest"
            if (string.Equals(normalized, "latest", StringComparison.OrdinalIgnoreCase))
            {
                version = LuaCompatibilityVersion.Latest;
                return true;
            }

            return false;
        }

        private static bool IsLuaVersionOption(string argument)
        {
            return string.Equals(argument, "-v", StringComparison.Ordinal)
                || string.Equals(argument, "--lua-version", StringComparison.Ordinal)
                || string.Equals(argument, "--version", StringComparison.Ordinal);
        }

        private static bool TryRunExecuteChunks(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return false;
            }

            List<string> inlineChunks = new();
            LuaCompatibilityVersion luaVersion = LuaCompatibilityVersion.Latest;
            int index = 0;

            while (index < args.Length)
            {
                string current = args[index];

                // Check for --lua-version
                if (IsLuaVersionOption(current))
                {
                    if (index + 1 >= args.Length)
                    {
                        Console.WriteLine(CliMessages.ProgramWrongSyntax);
                        ShowCmdLineHelp();
                        return true;
                    }

                    if (!TryParseLuaVersion(args[index + 1], out luaVersion))
                    {
                        Console.WriteLine(
                            $"Invalid Lua version: {args[index + 1]}. Valid values: 5.1, 5.2, 5.3, 5.4, 5.5, latest"
                        );
                        return true;
                    }

                    index += 2;
                    continue;
                }

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

            ScriptOptions options = new(Script.DefaultOptions)
            {
                CompatibilityVersion = luaVersion,
            };
            Script script = new(CoreModulePresets.Complete, options);

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

        private static bool IsKnownNonScriptOption(string argument)
        {
            // These options are handled elsewhere and should not be treated as script paths
            return string.Equals(argument, "-H", StringComparison.Ordinal)
                || string.Equals(argument, "--help", StringComparison.Ordinal)
                || string.Equals(argument, "/?", StringComparison.Ordinal)
                || string.Equals(argument, "-?", StringComparison.Ordinal)
                || string.Equals(argument, "-X", StringComparison.Ordinal)
                || string.Equals(argument, "-W", StringComparison.Ordinal);
        }

        private static bool TryRunScriptArgument(string[] args)
        {
            // Quick check: if the first argument is a known non-script option, don't try to run as script
            if (args.Length > 0 && IsKnownNonScriptOption(args[0]))
            {
                return false;
            }

            // First, extract any --lua-version option and find the script path
            LuaCompatibilityVersion explicitVersion = LuaCompatibilityVersion.Latest;
            bool hasExplicitVersion = false;
            string scriptPath = null;
            bool hasUnknownOptions = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (IsLuaVersionOption(args[i]))
                {
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine(CliMessages.ProgramWrongSyntax);
                        ShowCmdLineHelp();
                        return true;
                    }

                    if (!TryParseLuaVersion(args[i + 1], out explicitVersion))
                    {
                        Console.WriteLine(
                            $"Invalid Lua version: {args[i + 1]}. Valid values: 5.1, 5.2, 5.3, 5.4, 5.5, latest"
                        );
                        return true;
                    }

                    hasExplicitVersion = true;
                    i++; // Skip the version value
                }
                else if (args[i].Length > 0 && args[i][0] == '-')
                {
                    // Unknown option - let other handlers deal with it
                    hasUnknownOptions = true;
                }
                else if (scriptPath == null)
                {
                    scriptPath = args[i];
                }
            }

            // If there are unknown options, let other handlers deal with them
            if (hasUnknownOptions)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                return false;
            }

            string resolvedScriptPath = ResolveScriptPath(scriptPath);
            ScriptOptions options = new(Script.DefaultOptions);

            // Apply explicit version if provided; otherwise try to detect from mod.json
            if (hasExplicitVersion)
            {
                options.CompatibilityVersion = explicitVersion;
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
            Console.WriteLine(CliMessages.ProgramUsageLuaVersionSwitch);
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
