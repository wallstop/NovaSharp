namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
    using System.IO;
    using System.Security;
    using NovaSharp.Cli;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modding;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Utilities;

    /// <summary>
    /// CLI command that runs a Lua file from disk, applying mod manifests when present.
    /// </summary>
    internal sealed class RunCommand : ICommand
    {
        /// <inheritdoc />
        public string Name
        {
            get { return "run"; }
        }

        /// <inheritdoc />
        public void DisplayShortHelp()
        {
            Console.WriteLine(CliMessages.RunCommandShortHelp);
        }

        /// <inheritdoc />
        public void DisplayLongHelp()
        {
            Console.WriteLine(CliMessages.RunCommandLongHelp);
        }

        /// <inheritdoc />
        public void Execute(ShellContext context, string arguments)
        {
            if (arguments.Length == 0)
            {
                Console.WriteLine(CliMessages.RunCommandSyntax);
            }
            else
            {
                string resolvedPath = ResolveScriptPath(arguments);
                ScriptOptions options = new(context.Script.Options);
                bool manifestApplied = ModManifestCompatibility.TryApplyFromScriptPath(
                    resolvedPath,
                    options,
                    Script.GlobalOptions.CompatibilityVersion,
                    info => Console.WriteLine(CliMessages.ContextualCompatibilityInfo(info)),
                    warning => Console.WriteLine(CliMessages.CompatibilityWarning(warning))
                );

                if (!manifestApplied)
                {
                    LogCompatibilitySummary(resolvedPath, context.Script);
                    context.Script.DoFile(arguments);
                    return;
                }

                Script script = new(CoreModules.PresetComplete, options);
                LogCompatibilitySummary(resolvedPath, script);
                script.DoFile(resolvedPath);
            }
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

        private static void LogCompatibilitySummary(string scriptPath, Script script)
        {
            LuaCompatibilityProfile profile = script.CompatibilityProfile;
            Console.WriteLine(
                $"[compatibility] Running '{scriptPath}' with {profile.GetFeatureSummary()}"
            );
        }
    }
}
