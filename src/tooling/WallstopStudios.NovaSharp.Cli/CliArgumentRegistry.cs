namespace WallstopStudios.NovaSharp.Cli
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;

    /// <summary>
    /// Represents a CLI argument definition with metadata for help generation.
    /// </summary>
    internal sealed class CliArgumentDefinition
    {
        /// <summary>
        /// Short form of the argument (e.g., "-h", "-v").
        /// </summary>
        public string ShortForm { get; }

        /// <summary>
        /// Long form of the argument (e.g., "--help", "--lua-version").
        /// </summary>
        public string LongForm { get; }

        /// <summary>
        /// Human-readable description for help text.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Placeholder for the value if the argument takes one (e.g., "&lt;version&gt;").
        /// </summary>
        public string ValuePlaceholder { get; }

        /// <summary>
        /// Whether this argument requires a value.
        /// </summary>
        public bool RequiresValue { get; }

        /// <summary>
        /// Whether this argument causes the program to exit after processing (e.g., --help).
        /// </summary>
        public bool ExitsAfterProcessing { get; }

        /// <summary>
        /// Category for grouping in help output.
        /// </summary>
        public CliArgumentCategory Category { get; }

        public CliArgumentDefinition(
            string shortForm,
            string longForm,
            string description,
            string valuePlaceholder,
            bool requiresValue,
            bool exitsAfterProcessing,
            CliArgumentCategory category
        )
        {
            ShortForm = shortForm;
            LongForm = longForm;
            Description = description;
            ValuePlaceholder = valuePlaceholder;
            RequiresValue = requiresValue;
            ExitsAfterProcessing = exitsAfterProcessing;
            Category = category;
        }

        /// <summary>
        /// Returns true if the given argument string matches this definition.
        /// </summary>
        public bool Matches(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                return false;
            }

            if (
                !string.IsNullOrEmpty(ShortForm)
                && string.Equals(argument, ShortForm, StringComparison.Ordinal)
            )
            {
                return true;
            }

            if (
                !string.IsNullOrEmpty(LongForm)
                && string.Equals(argument, LongForm, StringComparison.Ordinal)
            )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Formats the argument for usage display (e.g., "-v, --lua-version &lt;version&gt;").
        /// </summary>
        public string FormatForUsage()
        {
            List<string> parts = new();

            if (!string.IsNullOrEmpty(ShortForm))
            {
                parts.Add(ShortForm);
            }

            if (!string.IsNullOrEmpty(LongForm))
            {
                parts.Add(LongForm);
            }

            string prefix = string.Join(", ", parts);

            if (!string.IsNullOrEmpty(ValuePlaceholder))
            {
                return $"{prefix} {ValuePlaceholder}";
            }

            return prefix;
        }
    }

    /// <summary>
    /// Categories for grouping CLI arguments in help output.
    /// </summary>
    internal enum CliArgumentCategory
    {
        /// <summary>Help and information options.</summary>
        Help = 0,

        /// <summary>Execution mode options.</summary>
        Execution = 1,

        /// <summary>Lua compatibility options.</summary>
        Compatibility = 2,

        /// <summary>Code generation/tooling options.</summary>
        Tooling = 3,
    }

    /// <summary>
    /// Result of parsing CLI arguments.
    /// </summary>
    internal sealed class CliParseResult
    {
        /// <summary>
        /// Whether parsing was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Error message if parsing failed.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Whether the program should exit after processing (e.g., --help was requested).
        /// </summary>
        public bool ShouldExit { get; }

        /// <summary>
        /// Exit code to use if ShouldExit is true.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// The mode of execution requested.
        /// </summary>
        public CliExecutionMode Mode { get; }

        /// <summary>
        /// Lua compatibility version if specified.
        /// </summary>
        public LuaCompatibilityVersion? LuaVersion { get; }

        /// <summary>
        /// Script path to execute (for Script mode).
        /// </summary>
        public string ScriptPath { get; }

        /// <summary>
        /// Inline code chunks to execute (for Execute mode).
        /// </summary>
        public IReadOnlyList<string> InlineChunks { get; }

        /// <summary>
        /// Hardwire arguments (for Hardwire mode).
        /// </summary>
        public HardwireArguments HardwireArgs { get; }

        /// <summary>
        /// REPL command to execute (for Command mode).
        /// </summary>
        public string ReplCommand { get; }

        private CliParseResult(
            bool success,
            string errorMessage,
            bool shouldExit,
            int exitCode,
            CliExecutionMode mode,
            LuaCompatibilityVersion? luaVersion,
            string scriptPath,
            IReadOnlyList<string> inlineChunks,
            HardwireArguments hardwireArgs,
            string replCommand
        )
        {
            Success = success;
            ErrorMessage = errorMessage;
            ShouldExit = shouldExit;
            ExitCode = exitCode;
            Mode = mode;
            LuaVersion = luaVersion;
            ScriptPath = scriptPath;
            InlineChunks = inlineChunks;
            HardwireArgs = hardwireArgs;
            ReplCommand = replCommand;
        }

        public static CliParseResult ForRepl(LuaCompatibilityVersion? version = null)
        {
            return new CliParseResult(
                success: true,
                errorMessage: null,
                shouldExit: false,
                exitCode: 0,
                mode: CliExecutionMode.Repl,
                luaVersion: version,
                scriptPath: null,
                inlineChunks: Array.Empty<string>(),
                hardwireArgs: null,
                replCommand: null
            );
        }

        public static CliParseResult ForHelp()
        {
            return new CliParseResult(
                success: true,
                errorMessage: null,
                shouldExit: true,
                exitCode: 0,
                mode: CliExecutionMode.Help,
                luaVersion: null,
                scriptPath: null,
                inlineChunks: Array.Empty<string>(),
                hardwireArgs: null,
                replCommand: null
            );
        }

        public static CliParseResult ForScript(string scriptPath, LuaCompatibilityVersion? version)
        {
            return new CliParseResult(
                success: true,
                errorMessage: null,
                shouldExit: true,
                exitCode: 0,
                mode: CliExecutionMode.Script,
                luaVersion: version,
                scriptPath: scriptPath,
                inlineChunks: Array.Empty<string>(),
                hardwireArgs: null,
                replCommand: null
            );
        }

        public static CliParseResult ForExecute(
            IReadOnlyList<string> chunks,
            LuaCompatibilityVersion? version
        )
        {
            return new CliParseResult(
                success: true,
                errorMessage: null,
                shouldExit: true,
                exitCode: 0,
                mode: CliExecutionMode.Execute,
                luaVersion: version,
                scriptPath: null,
                inlineChunks: chunks,
                hardwireArgs: null,
                replCommand: null
            );
        }

        public static CliParseResult ForHardwire(HardwireArguments args)
        {
            return new CliParseResult(
                success: true,
                errorMessage: null,
                shouldExit: true,
                exitCode: 0,
                mode: CliExecutionMode.Hardwire,
                luaVersion: null,
                scriptPath: null,
                inlineChunks: Array.Empty<string>(),
                hardwireArgs: args,
                replCommand: null
            );
        }

        public static CliParseResult ForCommand(string command)
        {
            return new CliParseResult(
                success: true,
                errorMessage: null,
                shouldExit: true,
                exitCode: 0,
                mode: CliExecutionMode.Command,
                luaVersion: null,
                scriptPath: null,
                inlineChunks: Array.Empty<string>(),
                hardwireArgs: null,
                replCommand: command
            );
        }

        public static CliParseResult ForError(string message)
        {
            return new CliParseResult(
                success: false,
                errorMessage: message,
                shouldExit: true,
                exitCode: 1,
                mode: CliExecutionMode.Repl,
                luaVersion: null,
                scriptPath: null,
                inlineChunks: Array.Empty<string>(),
                hardwireArgs: null,
                replCommand: null
            );
        }
    }

    /// <summary>
    /// Execution modes for the CLI.
    /// </summary>
    internal enum CliExecutionMode
    {
        /// <summary>Interactive REPL mode (default).</summary>
        Repl = 0,

        /// <summary>Display help and exit.</summary>
        Help = 1,

        /// <summary>Execute a script file.</summary>
        Script = 2,

        /// <summary>Execute inline code chunks via -e.</summary>
        Execute = 3,

        /// <summary>Generate hardwire descriptors.</summary>
        Hardwire = 4,

        /// <summary>Execute a REPL command via -X.</summary>
        Command = 5,
    }

    /// <summary>
    /// Arguments for the hardwire code generation mode.
    /// </summary>
    internal sealed class HardwireArguments
    {
        public string DumpFile { get; set; }
        public string DestinationFile { get; set; }
        public bool AllowInternals { get; set; }
        public bool UseVisualBasic { get; set; }
        public string ClassName { get; set; }
        public string NamespaceName { get; set; }
    }

    /// <summary>
    /// Centralized registry of all CLI arguments with descriptions, validation, and help generation.
    /// </summary>
    internal static class CliArgumentRegistry
    {
        /// <summary>
        /// Help argument (-H, --help).
        /// </summary>
        public static readonly CliArgumentDefinition Help = new(
            shortForm: "-H",
            longForm: "--help",
            description: "Display this help message and exit",
            valuePlaceholder: null,
            requiresValue: false,
            exitsAfterProcessing: true,
            category: CliArgumentCategory.Help
        );

        /// <summary>
        /// Alternative help arguments (/?, -?).
        /// </summary>
        public static readonly CliArgumentDefinition HelpAlt = new(
            shortForm: "-?",
            longForm: "/?",
            description: "Display this help message and exit (alternative)",
            valuePlaceholder: null,
            requiresValue: false,
            exitsAfterProcessing: true,
            category: CliArgumentCategory.Help
        );

        /// <summary>
        /// Lua version argument (-v, --lua-version, --version).
        /// </summary>
        public static readonly CliArgumentDefinition LuaVersion = new(
            shortForm: "-v",
            longForm: "--lua-version",
            description: "Set the Lua compatibility version",
            valuePlaceholder: "<version>",
            requiresValue: true,
            exitsAfterProcessing: false,
            category: CliArgumentCategory.Compatibility
        );

        /// <summary>
        /// Execute inline code argument (-e, --execute).
        /// </summary>
        public static readonly CliArgumentDefinition Execute = new(
            shortForm: "-e",
            longForm: "--execute",
            description: "Execute inline Lua code (can be specified multiple times)",
            valuePlaceholder: "<code>",
            requiresValue: true,
            exitsAfterProcessing: true,
            category: CliArgumentCategory.Execution
        );

        /// <summary>
        /// REPL command argument (-X).
        /// </summary>
        public static readonly CliArgumentDefinition Command = new(
            shortForm: "-X",
            longForm: null,
            description: "Execute a REPL command and exit",
            valuePlaceholder: "<command>",
            requiresValue: true,
            exitsAfterProcessing: true,
            category: CliArgumentCategory.Execution
        );

        /// <summary>
        /// Hardwire generation argument (-W).
        /// </summary>
        public static readonly CliArgumentDefinition Hardwire = new(
            shortForm: "-W",
            longForm: null,
            description: "Generate hardwire descriptors from a dump table",
            valuePlaceholder: "<dumpfile> <destfile>",
            requiresValue: true,
            exitsAfterProcessing: true,
            category: CliArgumentCategory.Tooling
        );

        /// <summary>
        /// Hardwire internals flag (--internals).
        /// </summary>
        public static readonly CliArgumentDefinition HardwireInternals = new(
            shortForm: null,
            longForm: "--internals",
            description: "Include internal members in hardwire generation",
            valuePlaceholder: null,
            requiresValue: false,
            exitsAfterProcessing: false,
            category: CliArgumentCategory.Tooling
        );

        /// <summary>
        /// Hardwire VB flag (--vb).
        /// </summary>
        public static readonly CliArgumentDefinition HardwireVb = new(
            shortForm: null,
            longForm: "--vb",
            description: "Generate Visual Basic code instead of C#",
            valuePlaceholder: null,
            requiresValue: false,
            exitsAfterProcessing: false,
            category: CliArgumentCategory.Tooling
        );

        /// <summary>
        /// Hardwire class name (--class:).
        /// </summary>
        public static readonly CliArgumentDefinition HardwireClass = new(
            shortForm: null,
            longForm: "--class",
            description: "Set the generated class name for hardwire",
            valuePlaceholder: "<name>",
            requiresValue: true,
            exitsAfterProcessing: false,
            category: CliArgumentCategory.Tooling
        );

        /// <summary>
        /// Hardwire namespace (--namespace:).
        /// </summary>
        public static readonly CliArgumentDefinition HardwireNamespace = new(
            shortForm: null,
            longForm: "--namespace",
            description: "Set the generated namespace for hardwire",
            valuePlaceholder: "<name>",
            requiresValue: true,
            exitsAfterProcessing: false,
            category: CliArgumentCategory.Tooling
        );

        /// <summary>
        /// All registered argument definitions for enumeration.
        /// </summary>
        private static readonly CliArgumentDefinition[] AllDefinitions =
        {
            Help,
            HelpAlt,
            LuaVersion,
            Execute,
            Command,
            Hardwire,
            HardwireInternals,
            HardwireVb,
            HardwireClass,
            HardwireNamespace,
        };

        /// <summary>
        /// Valid Lua version strings for error messages.
        /// </summary>
        public const string ValidLuaVersions = "5.1, 5.2, 5.3, 5.4, 5.5, latest";

        /// <summary>
        /// Parses the command-line arguments into a structured result.
        /// </summary>
        /// <param name="args">Raw command-line arguments.</param>
        /// <returns>Parse result with execution mode and parameters.</returns>
        public static CliParseResult Parse(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return CliParseResult.ForRepl();
            }

            // Check for help first
            if (IsHelpArgument(args[0]))
            {
                return CliParseResult.ForHelp();
            }

            // Check for REPL command (-X)
            if (Command.Matches(args[0]))
            {
                return ParseCommandMode(args);
            }

            // Check for hardwire mode (-W)
            if (Hardwire.Matches(args[0]))
            {
                return ParseHardwireMode(args);
            }

            // Check for execute mode (-e)
            if (Execute.Matches(args[0]) || IsLuaVersionArgument(args[0]))
            {
                return ParseExecuteOrScriptMode(args);
            }

            // Otherwise try to parse as script path with optional version
            return ParseScriptMode(args);
        }

        /// <summary>
        /// Tries to parse a Lua version string into a <see cref="LuaCompatibilityVersion"/>.
        /// </summary>
        /// <param name="versionString">The version string to parse.</param>
        /// <param name="version">The parsed version.</param>
        /// <returns>True if parsing succeeded.</returns>
        public static bool TryParseLuaVersion(
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

        /// <summary>
        /// Generates comprehensive help text for all CLI arguments.
        /// </summary>
        public static string GenerateHelpText()
        {
            List<string> lines = new();

            lines.Add("NovaSharp - A Lua interpreter for .NET");
            lines.Add(string.Empty);
            lines.Add("USAGE:");
            lines.Add("  nova [OPTIONS] [SCRIPT]");
            lines.Add("  nova -e <code> [-e <code>...] [-v <version>]");
            lines.Add(
                "  nova -W <dumpfile> <destfile> [--internals] [--vb] [--class:<name>] [--namespace:<name>]"
            );
            lines.Add("  nova -X <command>");
            lines.Add(string.Empty);

            // Group arguments by category
            Dictionary<CliArgumentCategory, List<CliArgumentDefinition>> byCategory = new();
            foreach (CliArgumentDefinition def in AllDefinitions)
            {
                if (!byCategory.TryGetValue(def.Category, out List<CliArgumentDefinition> list))
                {
                    list = new List<CliArgumentDefinition>();
                    byCategory[def.Category] = list;
                }

                list.Add(def);
            }

            // Output each category
            string[] categoryNames =
            {
                "HELP OPTIONS:",
                "EXECUTION OPTIONS:",
                "COMPATIBILITY OPTIONS:",
                "TOOLING OPTIONS:",
            };

#if NET8_0_OR_GREATER
            foreach (CliArgumentCategory category in Enum.GetValues<CliArgumentCategory>())
#else
            foreach (
                CliArgumentCategory category in (CliArgumentCategory[])
                    Enum.GetValues(typeof(CliArgumentCategory))
            )
#endif
            {
                if (
                    !byCategory.TryGetValue(category, out List<CliArgumentDefinition> defs)
                    || defs.Count == 0
                )
                {
                    continue;
                }

                lines.Add(categoryNames[(int)category]);

                foreach (CliArgumentDefinition def in defs)
                {
                    string usage = def.FormatForUsage();
                    lines.Add($"  {usage, -30} {def.Description}");
                }

                lines.Add(string.Empty);
            }

            lines.Add("LUA VERSIONS:");
            lines.Add($"  Valid values: {ValidLuaVersions}");
            lines.Add(string.Empty);
            lines.Add("EXAMPLES:");
            lines.Add("  nova script.lua                    Run a Lua script");
            lines.Add("  nova -v 5.1 script.lua             Run script in Lua 5.1 mode");
            lines.Add("  nova -e \"print('Hello')\"           Execute inline code");
            lines.Add("  nova -e \"x=1\" -e \"print(x)\"        Execute multiple inline chunks");
            lines.Add("  nova                               Start interactive REPL");
            lines.Add(string.Empty);

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Gets all argument definitions for enumeration.
        /// </summary>
        public static IEnumerable<CliArgumentDefinition> GetAllDefinitions()
        {
            return AllDefinitions;
        }

        private static bool IsHelpArgument(string arg)
        {
            return Help.Matches(arg) || HelpAlt.Matches(arg);
        }

        private static bool IsLuaVersionArgument(string arg)
        {
            return LuaVersion.Matches(arg)
                || string.Equals(arg, "--version", StringComparison.Ordinal);
        }

        private static bool IsExecuteArgument(string arg)
        {
            return Execute.Matches(arg);
        }

        private static CliParseResult ParseCommandMode(string[] args)
        {
            if (args.Length < 2)
            {
                return CliParseResult.ForError("Missing command argument for -X");
            }

            return CliParseResult.ForCommand(args[1]);
        }

        private static CliParseResult ParseHardwireMode(string[] args)
        {
            HardwireArguments hwArgs = new();
            int i = 1;

            while (i < args.Length)
            {
                string arg = args[i];

                if (HardwireInternals.Matches(arg))
                {
                    hwArgs.AllowInternals = true;
                    i++;
                }
                else if (HardwireVb.Matches(arg))
                {
                    hwArgs.UseVisualBasic = true;
                    i++;
                }
                else if (arg.StartsWith("--class:", StringComparison.Ordinal))
                {
                    hwArgs.ClassName = arg.Substring("--class:".Length);
                    i++;
                }
                else if (arg.StartsWith("--namespace:", StringComparison.Ordinal))
                {
                    hwArgs.NamespaceName = arg.Substring("--namespace:".Length);
                    i++;
                }
                else if (hwArgs.DumpFile == null)
                {
                    hwArgs.DumpFile = arg;
                    i++;
                }
                else if (hwArgs.DestinationFile == null)
                {
                    hwArgs.DestinationFile = arg;
                    i++;
                }
                else
                {
                    return CliParseResult.ForError($"Unexpected argument: {arg}");
                }
            }

            if (
                string.IsNullOrEmpty(hwArgs.DumpFile)
                || string.IsNullOrEmpty(hwArgs.DestinationFile)
            )
            {
                return CliParseResult.ForError(
                    "Hardwire mode requires <dumpfile> and <destfile> arguments"
                );
            }

            return CliParseResult.ForHardwire(hwArgs);
        }

        private static CliParseResult ParseExecuteOrScriptMode(string[] args)
        {
            List<string> inlineChunks = new();
            LuaCompatibilityVersion? version = null;
            string scriptPath = null;
            int i = 0;

            while (i < args.Length)
            {
                string arg = args[i];

                if (IsLuaVersionArgument(arg))
                {
                    if (i + 1 >= args.Length)
                    {
                        return CliParseResult.ForError($"Missing value for {arg}");
                    }

                    if (!TryParseLuaVersion(args[i + 1], out LuaCompatibilityVersion parsedVersion))
                    {
                        return CliParseResult.ForError(
                            $"Invalid Lua version: {args[i + 1]}. Valid values: {ValidLuaVersions}"
                        );
                    }

                    version = parsedVersion;
                    i += 2;
                }
                else if (IsExecuteArgument(arg))
                {
                    if (i + 1 >= args.Length)
                    {
                        return CliParseResult.ForError($"Missing code argument for {arg}");
                    }

                    inlineChunks.Add(args[i + 1]);
                    i += 2;
                }
                else if (arg.Length > 0 && arg[0] == '-')
                {
                    return CliParseResult.ForError($"Unknown option: {arg}");
                }
                else
                {
                    // Positional argument - treat as script path
                    if (scriptPath != null)
                    {
                        return CliParseResult.ForError($"Unexpected argument: {arg}");
                    }

                    scriptPath = arg;
                    i++;
                }
            }

            // If we have inline chunks, use execute mode
            if (inlineChunks.Count > 0)
            {
                if (scriptPath != null)
                {
                    return CliParseResult.ForError("Cannot specify both -e and a script path");
                }

                return CliParseResult.ForExecute(inlineChunks, version);
            }

            // If we have a script path, use script mode
            if (scriptPath != null)
            {
                return CliParseResult.ForScript(scriptPath, version);
            }

            // No chunks and no script - start REPL with version if specified
            return CliParseResult.ForRepl(version);
        }

        private static CliParseResult ParseScriptMode(string[] args)
        {
            LuaCompatibilityVersion? version = null;
            string scriptPath = null;
            int i = 0;

            while (i < args.Length)
            {
                string arg = args[i];

                if (IsLuaVersionArgument(arg))
                {
                    if (i + 1 >= args.Length)
                    {
                        return CliParseResult.ForError($"Missing value for {arg}");
                    }

                    if (!TryParseLuaVersion(args[i + 1], out LuaCompatibilityVersion parsedVersion))
                    {
                        return CliParseResult.ForError(
                            $"Invalid Lua version: {args[i + 1]}. Valid values: {ValidLuaVersions}"
                        );
                    }

                    version = parsedVersion;
                    i += 2;
                }
                else if (arg.Length > 0 && arg[0] == '-')
                {
                    // Unknown option - let other handlers deal with it or error
                    return CliParseResult.ForError($"Unknown option: {arg}");
                }
                else
                {
                    if (scriptPath != null)
                    {
                        return CliParseResult.ForError($"Unexpected argument: {arg}");
                    }

                    scriptPath = arg;
                    i++;
                }
            }

            if (scriptPath == null)
            {
                return CliParseResult.ForRepl(version);
            }

            return CliParseResult.ForScript(scriptPath, version);
        }
    }
}
