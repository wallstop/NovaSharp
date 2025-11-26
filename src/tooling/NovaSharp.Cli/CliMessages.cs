namespace NovaSharp.Cli
{
    internal static class CliMessages
    {
        internal static string RunCommandShortHelp =>
            "run <filename> - Executes the specified Lua script";
        internal static string RunCommandLongHelp =>
            "run <filename> - Executes the specified Lua script.";
        internal static string RunCommandSyntax => "Syntax : !run <file>";

        internal static string CompileCommandShortHelp =>
            "compile <filename> - Compiles the file in a binary format";
        internal static string CompileCommandLongHelp =>
            "compile <filename> - Compiles the file in a binary format. The destination filename will be appended with '-compiled'.";

        internal static string CompileCommandSuccess(string targetPath) =>
            $"[compile] Wrote binary chunk to '{targetPath}'.";

        internal static string CompileCommandFailure(string sourcePath, string errorMessage) =>
            $"[compile] Failed to compile '{sourcePath}': {errorMessage}";

        internal static string HelpCommandShortHelp =>
            "help [command] - gets the list of possible commands or help about the specified command";
        internal static string HelpCommandCommandListHeading => "Commands:";
        internal static string HelpCommandPrimaryInstruction =>
            "Type Lua code to execute Lua code (multilines are accepted)";
        internal static string HelpCommandSecondaryInstruction =>
            "or type one of the following commands to execute them.";
        internal static string HelpCommandCommandPrefix => "  !";
        internal static string HelpCommandCompatibilitySummary =>
            "Use Script.Options.CompatibilityVersion or set luaCompatibility in mod.json to change it.";

        internal static string HelpCommandCommandNotFound(string command) =>
            $"Command '{command}' not found.";

        internal static string RegisterCommandShortHelp =>
            "register [type] - register a CLR type or prints a list of registered types";
        internal static string RegisterCommandLongHelp =>
            "register [type] - register a CLR type or prints a list of registered types. Use makestatic('type') to make a static instance.";

        internal static string RegisterCommandTypeNotFound(string typeName) =>
            $"Type {typeName} not found.";

        internal static string HardwireCommandShortHelp =>
            "hardwire - Creates hardwire descriptors from a dump table (interactive). ";
        internal static string HardwireCommandLongHelp =>
            "hardwire - Creates hardwire descriptors from a dump table (interactive). ";
        internal static string HardwireCommandAbortHint => "At any question, type #quit to abort.";
        internal static string HardwireLanguagePrompt => "Language, cs or vb ? [cs] : ";
        internal static string HardwireLanguageValidation => "Must be 'cs' or 'vb'.";
        internal static string HardwireDumpPrompt => "Lua dump table file: ";
        internal static string HardwireDestinationPrompt => "Destination file: ";
        internal static string HardwireInternalsPrompt => "Allow internals y/n ? [y]: ";
        internal static string HardwireNamespacePrompt => "Namespace ? [HardwiredClasses]: ";
        internal static string HardwireClassPrompt => "Class ? [HardwireTypes]: ";
        internal static string HardwireIdentifierValidation => "Not a valid identifier.";
        internal static string HardwireMissingFile => "File does not exists.";

        internal static string HardwireErrorLog(string message) => $"[EE] - {message}";

        internal static string HardwireWarningLog(string message) => $"[ww] - {message}";

        internal static string HardwireInfoLog(string message) => $"[ii] - {message}";

        internal static string HardwireGenerationSummary(int errors, int warnings) =>
            $"done: {errors} errors, {warnings} warnings.";

        internal static string HardwireInternalError(string message) =>
            $"Internal error : {message}";

        internal static string DebugCommandShortHelp => "debug - Starts the interactive debugger";
        internal static string DebugCommandLongHelp =>
            "debug - Starts the interactive debugger. Requires a web browser with Flash installed.";

        internal static string ExitCommandShortHelp => "exit - Exits the interpreter";
        internal static string ExitCommandLongHelp => "exit - Exits the interpreter";

        internal static string CommandManagerEmptyCommand => "Invalid command ''.";

        internal static string CommandManagerInvalidCommand(string command) =>
            $"Invalid command '{command}'.";

        internal static string ProgramTypeNotFound(string typeName) =>
            $"Type '{typeName}' not found.";

        internal static string ProgramWrongSyntax => "Wrong syntax.";
        internal static string ProgramUsageShort =>
            "usage: NovaSharp [-H | --help | -X \"command\" | -W <dumpfile> <destfile> [--internals] [--vb] | <script>]";
        internal static string ProgramUsageLong =>
            "usage: NovaSharp [-H | --help | -X \"command\" | -W <dumpfile> <destfile> [--internals] [--vb] [--class:<name>] [--namespace:<name>] | <script>]";
        internal static string ProgramUsageHelpSwitch => "-H : shows this help";
        internal static string ProgramUsageExecuteSwitch => "-X : executes the specified command";
        internal static string ProgramUsageHardwireSwitch => "-W : creates hardwire descriptors";
        internal static string ProgramWelcome => "Welcome.";
        internal static string ProgramCompatibilityUsage =>
            "Type Lua code to execute it or type !help to see help on commands.";
        internal static string ProgramCompatibilityHint =>
            "[compatibility] Use Script.Options.CompatibilityVersion or set luaCompatibility in mod.json to change it.";

        internal static string ProgramActiveProfile(string summary) =>
            $"[compatibility] Active profile: {summary}";

        internal static string ProgramRunningScript(string path, string summary) =>
            $"[compatibility] Running '{path}' with {summary}";

        internal static string ContextualCompatibilityInfo(string info) =>
            $"[compatibility] {info}";

        internal static string CompatibilityWarning(string warning) => $"[compatibility] {warning}";
    }
}
