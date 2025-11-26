namespace NovaSharp.Cli
{
    /// <summary>
    /// Provides the CLI strings shown by the NovaSharp interpreter shell and helper commands.
    /// </summary>
    internal static class CliMessages
    {
        /// <summary>
        /// Short help description presented for the run command in the command list.
        /// </summary>
        internal static string RunCommandShortHelp =>
            "run <filename> - Executes the specified Lua script";

        /// <summary>
        /// Detailed help text for the run command.
        /// </summary>
        internal static string RunCommandLongHelp =>
            "run <filename> - Executes the specified Lua script.";

        /// <summary>
        /// Syntax hint displayed when the run command usage is shown.
        /// </summary>
        internal static string RunCommandSyntax => "Syntax : !run <file>";

        /// <summary>
        /// Short description of the compile command for the help list.
        /// </summary>
        internal static string CompileCommandShortHelp =>
            "compile <filename> - Compiles the file in a binary format";

        /// <summary>
        /// Detailed help text for the compile command, including destination info.
        /// </summary>
        internal static string CompileCommandLongHelp =>
            "compile <filename> - Compiles the file in a binary format. The destination filename will be appended with '-compiled'.";

        /// <summary>
        /// Formats the success message after a compilation writes a binary chunk.
        /// </summary>
        /// <param name="targetPath">Destination path that received the compiled chunk.</param>
        /// <returns>Success message describing where the chunk was written.</returns>
        internal static string CompileCommandSuccess(string targetPath) =>
            $"[compile] Wrote binary chunk to '{targetPath}'.";

        /// <summary>
        /// Formats the failure message emitted when compilation fails.
        /// </summary>
        /// <param name="sourcePath">Lua source file that failed to compile.</param>
        /// <param name="errorMessage">Error returned by the compiler.</param>
        /// <returns>Error string that surfaces the failure details.</returns>
        internal static string CompileCommandFailure(string sourcePath, string errorMessage) =>
            $"[compile] Failed to compile '{sourcePath}': {errorMessage}";

        /// <summary>
        /// Short help entry describing the help command itself.
        /// </summary>
        internal static string HelpCommandShortHelp =>
            "help [command] - gets the list of possible commands or help about the specified command";

        /// <summary>
        /// Heading used for the command list in the help output.
        /// </summary>
        internal static string HelpCommandCommandListHeading => "Commands:";

        /// <summary>
        /// First instruction line shown when the user invokes help.
        /// </summary>
        internal static string HelpCommandPrimaryInstruction =>
            "Type Lua code to execute Lua code (multilines are accepted)";

        /// <summary>
        /// Second instruction line shown in help explaining how to run commands.
        /// </summary>
        internal static string HelpCommandSecondaryInstruction =>
            "or type one of the following commands to execute them.";

        /// <summary>
        /// Prefix applied to each command entry inside the help list.
        /// </summary>
        internal static string HelpCommandCommandPrefix => "  !";

        /// <summary>
        /// Compatibility reminder appended to the help output.
        /// </summary>
        internal static string HelpCommandCompatibilitySummary =>
            "Use Script.Options.CompatibilityVersion or set luaCompatibility in mod.json to change it.";

        /// <summary>
        /// Message emitted when the requested help command cannot be found.
        /// </summary>
        /// <param name="command">Command requested by the user.</param>
        /// <returns>Error text that notes the missing command.</returns>
        internal static string HelpCommandCommandNotFound(string command) =>
            $"Command '{command}' not found.";

        /// <summary>
        /// Short help information for the register command.
        /// </summary>
        internal static string RegisterCommandShortHelp =>
            "register [type] - register a CLR type or prints a list of registered types";

        /// <summary>
        /// Detailed register command description displayed in help.
        /// </summary>
        internal static string RegisterCommandLongHelp =>
            "register [type] - register a CLR type or prints a list of registered types. Use makestatic('type') to make a static instance.";

        /// <summary>
        /// Error shown when the requested CLR type cannot be located during registration.
        /// </summary>
        /// <param name="typeName">Fully qualified type name provided by the user.</param>
        /// <returns>Error message indicating the missing type.</returns>
        internal static string RegisterCommandTypeNotFound(string typeName) =>
            $"Type {typeName} not found.";

        /// <summary>
        /// Short help entry describing the hardwire command.
        /// </summary>
        internal static string HardwireCommandShortHelp =>
            "hardwire - Creates hardwire descriptors from a dump table (interactive). ";

        /// <summary>
        /// Detailed description for the hardwire command shown in help.
        /// </summary>
        internal static string HardwireCommandLongHelp =>
            "hardwire - Creates hardwire descriptors from a dump table (interactive). ";

        /// <summary>
        /// Hint that informs users they can quit the hardwire wizard.
        /// </summary>
        internal static string HardwireCommandAbortHint => "At any question, type #quit to abort.";

        /// <summary>
        /// Prompt asking the user to select the hardwire target language.
        /// </summary>
        internal static string HardwireLanguagePrompt => "Language, cs or vb ? [cs] : ";

        /// <summary>
        /// Validation message enforcing the accepted language values.
        /// </summary>
        internal static string HardwireLanguageValidation => "Must be 'cs' or 'vb'.";

        /// <summary>
        /// Prompt requesting the Lua dump table file path.
        /// </summary>
        internal static string HardwireDumpPrompt => "Lua dump table file: ";

        /// <summary>
        /// Prompt requesting the destination filename for generated descriptors.
        /// </summary>
        internal static string HardwireDestinationPrompt => "Destination file: ";

        /// <summary>
        /// Prompt asking whether internal members should be exposed during generation.
        /// </summary>
        internal static string HardwireInternalsPrompt => "Allow internals y/n ? [y]: ";

        /// <summary>
        /// Prompt requesting the namespace for generated hardwire types.
        /// </summary>
        internal static string HardwireNamespacePrompt => "Namespace ? [HardwiredClasses]: ";

        /// <summary>
        /// Prompt asking for the generated class name used by hardwire.
        /// </summary>
        internal static string HardwireClassPrompt => "Class ? [HardwireTypes]: ";

        /// <summary>
        /// Validation error displayed when the provided identifier is invalid.
        /// </summary>
        internal static string HardwireIdentifierValidation => "Not a valid identifier.";

        /// <summary>
        /// Error shown when the requested dump file cannot be found.
        /// </summary>
        internal static string HardwireMissingFile => "File does not exists.";

        /// <summary>
        /// Formats a hardwire error log entry.
        /// </summary>
        /// <param name="message">Error details to report.</param>
        /// <returns>Formatted error string.</returns>
        internal static string HardwireErrorLog(string message) => $"[EE] - {message}";

        /// <summary>
        /// Formats a hardwire warning log entry.
        /// </summary>
        /// <param name="message">Warning details to report.</param>
        /// <returns>Formatted warning string.</returns>
        internal static string HardwireWarningLog(string message) => $"[ww] - {message}";

        /// <summary>
        /// Formats a hardwire informational log entry.
        /// </summary>
        /// <param name="message">Informational text to emit.</param>
        /// <returns>Formatted informational string.</returns>
        internal static string HardwireInfoLog(string message) => $"[ii] - {message}";

        /// <summary>
        /// Produces the completion summary emitted after generation finishes.
        /// </summary>
        /// <param name="errors">Number of errors recorded during generation.</param>
        /// <param name="warnings">Number of warnings recorded during generation.</param>
        /// <returns>Summary text describing the result counts.</returns>
        internal static string HardwireGenerationSummary(int errors, int warnings) =>
            $"done: {errors} errors, {warnings} warnings.";

        /// <summary>
        /// Formats the internal error string generated when the hardwire process fails unexpectedly.
        /// </summary>
        /// <param name="message">Detailed error information.</param>
        /// <returns>Formatted internal error message.</returns>
        internal static string HardwireInternalError(string message) =>
            $"Internal error : {message}";

        /// <summary>
        /// Short help entry for the debugger command.
        /// </summary>
        internal static string DebugCommandShortHelp => "debug - Starts the interactive debugger";

        /// <summary>
        /// Detailed description for the debugger command, including prerequisites.
        /// </summary>
        internal static string DebugCommandLongHelp =>
            "debug - Starts the interactive debugger. Requires a web browser with Flash installed.";

        /// <summary>
        /// Short help entry describing the exit command.
        /// </summary>
        internal static string ExitCommandShortHelp => "exit - Exits the interpreter";

        /// <summary>
        /// Detailed description for the exit command.
        /// </summary>
        internal static string ExitCommandLongHelp => "exit - Exits the interpreter";

        /// <summary>
        /// Message emitted when the user submits an empty command.
        /// </summary>
        internal static string CommandManagerEmptyCommand => "Invalid command ''.";

        /// <summary>
        /// Formats the message shown when an invalid command is entered.
        /// </summary>
        /// <param name="command">Command text typed by the user.</param>
        /// <returns>String describing the invalid command.</returns>
        internal static string CommandManagerInvalidCommand(string command) =>
            $"Invalid command '{command}'.";

        /// <summary>
        /// Formats the error message shown when a requested CLR type is missing.
        /// </summary>
        /// <param name="typeName">Name of the CLR type that could not be found.</param>
        /// <returns>Error describing the missing type.</returns>
        internal static string ProgramTypeNotFound(string typeName) =>
            $"Type '{typeName}' not found.";

        /// <summary>
        /// Generic syntax error shown when CLI arguments are invalid.
        /// </summary>
        internal static string ProgramWrongSyntax => "Wrong syntax.";

        /// <summary>
        /// Short usage line rendered in help output for the CLI program.
        /// </summary>
        internal static string ProgramUsageShort =>
            "usage: NovaSharp [-H | --help | -X \"command\" | -W <dumpfile> <destfile> [--internals] [--vb] | <script>]";

        /// <summary>
        /// Detailed usage instructions describing all CLI switches.
        /// </summary>
        internal static string ProgramUsageLong =>
            "usage: NovaSharp [-H | --help | -X \"command\" | -W <dumpfile> <destfile> [--internals] [--vb] [--class:<name>] [--namespace:<name>] | <script>]";

        /// <summary>
        /// Description for the help switch displayed in CLI usage.
        /// </summary>
        internal static string ProgramUsageHelpSwitch => "-H : shows this help";

        /// <summary>
        /// Description for the execute switch displayed in CLI usage.
        /// </summary>
        internal static string ProgramUsageExecuteSwitch => "-X : executes the specified command";

        /// <summary>
        /// Description for the hardwire switch displayed in CLI usage.
        /// </summary>
        internal static string ProgramUsageHardwireSwitch => "-W : creates hardwire descriptors";

        /// <summary>
        /// Welcome banner emitted when the interpreter starts.
        /// </summary>
        internal static string ProgramWelcome => "Welcome.";

        /// <summary>
        /// Instruction reminding users how to run Lua code or view help.
        /// </summary>
        internal static string ProgramCompatibilityUsage =>
            "Type Lua code to execute it or type !help to see help on commands.";

        /// <summary>
        /// Compatibility hint displayed in the REPL after startup.
        /// </summary>
        internal static string ProgramCompatibilityHint =>
            "[compatibility] Use Script.Options.CompatibilityVersion or set luaCompatibility in mod.json to change it.";

        /// <summary>
        /// Formats the message describing the currently active compatibility profile.
        /// </summary>
        /// <param name="summary">Summary describing the compatibility mode.</param>
        /// <returns>String describing the profile.</returns>
        internal static string ProgramActiveProfile(string summary) =>
            $"[compatibility] Active profile: {summary}";

        /// <summary>
        /// Formats the message shown when a script is executed together with compatibility info.
        /// </summary>
        /// <param name="path">Path to the Lua script being run.</param>
        /// <param name="summary">Compatibility summary for the current execution.</param>
        /// <returns>Status string describing the running script.</returns>
        internal static string ProgramRunningScript(string path, string summary) =>
            $"[compatibility] Running '{path}' with {summary}";

        /// <summary>
        /// Formats an informational compatibility message.
        /// </summary>
        /// <param name="info">Message describing the compatibility event.</param>
        /// <returns>Compatibility information string.</returns>
        internal static string ContextualCompatibilityInfo(string info) =>
            $"[compatibility] {info}";

        /// <summary>
        /// Formats a compatibility warning message.
        /// </summary>
        /// <param name="warning">Warning text to emit.</param>
        /// <returns>Compatibility warning string.</returns>
        internal static string CompatibilityWarning(string warning) => $"[compatibility] {warning}";
    }
}
