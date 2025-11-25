namespace NovaSharp.Cli
{
    using System;
    using System.IO;
    using NovaSharp.Cli.Commands;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modding;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.REPL;
    using NovaSharp.Interpreter.Utilities;

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
                Console.WriteLine("Type '{0}' not found.", type);
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
                    Console.WriteLine("{0}", result);
                }
            }
            catch (InterpreterException ex)
            {
                Console.WriteLine("{0}", ex.DecoratedMessage ?? ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}", ex.Message);
            }
        }

        private static void Banner(Script script)
        {
            Console.WriteLine(Script.GetBanner("Console"));
            Console.WriteLine();
            LuaCompatibilityProfile profile = script.CompatibilityProfile;
            Console.WriteLine($"[compatibility] Active profile: {profile.GetFeatureSummary()}");
            Console.WriteLine(
                "[compatibility] Use Script.Options.CompatibilityVersion or set luaCompatibility in mod.json to change it."
            );
            Console.WriteLine(
                "Type Lua code to execute it or type !help to see help on commands.\n"
            );
            Console.WriteLine("Welcome.\n");
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
                    Console.WriteLine("Wrong syntax.");
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
                    Console.WriteLine("Wrong syntax.");
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
                info => Console.WriteLine($"[compatibility] {info}"),
                warning => Console.WriteLine($"[compatibility] {warning}")
            );

            Script script = new(CoreModules.PresetComplete, options);
            Console.WriteLine(
                $"[compatibility] Running '{resolvedScriptPath}' with {script.CompatibilityProfile.GetFeatureSummary()}"
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
            catch (Exception)
            {
                return candidate;
            }
        }

        private static void ShowCmdLineHelpBig()
        {
            Console.WriteLine(
                "usage: NovaSharp [-H | --help | -X \"command\" | -W <dumpfile> <destfile> [--internals] [--vb] [--class:<name>] [--namespace:<name>] | <script>]"
            );
            Console.WriteLine();
            Console.WriteLine("-H : shows this help");
            Console.WriteLine("-X : executes the specified command");
            Console.WriteLine("-W : creates hardwire descriptors");
            Console.WriteLine();
        }

        private static void ShowCmdLineHelp()
        {
            Console.WriteLine(
                "usage: NovaSharp [-H | --help | -X \"command\" | -W <dumpfile> <destfile> [--internals] [--vb] | <script>]"
            );
        }

        private static void ExecuteCommand(ShellContext shellContext, string cmdline)
        {
            CommandManager.Execute(shellContext, cmdline);

            if (shellContext.IsExitRequested)
            {
                Environment.Exit(shellContext.ExitCode);
            }
        }
    }
}
