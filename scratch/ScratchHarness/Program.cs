using System;
using System.IO;
using System.Runtime.InteropServices;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.DataTypes;
using NovaSharp.Interpreter.Loaders;
using NovaSharp.Interpreter.Modules;

string suite = args.Length > 0 ? args[0] : "TestMore/DataTypes/108-userdata.t";
try
{
    RunTapSuite(suite);
    Console.WriteLine($"Suite completed: {suite}");
}
catch (NovaSharp.Interpreter.Errors.ScriptRuntimeException sre)
{
    Console.WriteLine($"Suite threw runtime error: {sre.DecoratedMessage}");
    Console.WriteLine(sre.StackTrace);
}
catch (Exception ex)
{
    Console.WriteLine($"Suite threw: {ex}");
}

static void RunTapSuite(string relativePath)
{
    ScriptOptions options = new(Script.DefaultOptions)
    {
        DebugPrint = value => Console.WriteLine(value?.TrimEnd()),
        UseLuaErrorLocations = true,
        CompatibilityVersion = Script.GlobalOptions.CompatibilityVersion,
        ForceUtcDateTime = true,
    };

    Script script = new(CoreModules.PresetComplete, options);
    if (script.Options.ScriptLoader is not ScriptLoaderBase loader)
    {
        script.Options.ScriptLoader = new FileSystemScriptLoader();
        loader = (ScriptLoaderBase)script.Options.ScriptLoader;
    }

    ConfigureScriptLoader(loader);
    SeedTapGlobals(script);
    string friendlyName = relativePath.Replace('\\', '/');
    script.DoFile(GetAbsoluteTestPath(relativePath), null, friendlyName);
}

static void ConfigureScriptLoader(ScriptLoaderBase loader)
{
    string testDirectory = AppContext.BaseDirectory;
    string modulesDirectory = Path.Combine(testDirectory, "TestMore", "Modules")
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        .Replace('\\', '/');

    loader.ModulePaths = new[] { $"{modulesDirectory}/?", $"{modulesDirectory}/?.lua" };
}

static string GetAbsoluteTestPath(string relativePath)
{
    string testDirectory = AppContext.BaseDirectory;
    string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
    return Path.Combine(testDirectory, normalized);
}

static void SeedTapGlobals(Script script)
{
    string luaExecutable = GetLuaExecutableHint();

    Table argTable = new(script);
    argTable.Set(DynValue.NewNumber(-1), DynValue.NewString(luaExecutable));
    script.Globals.Set("arg", DynValue.NewTable(argTable));

    Table platform = new(script);
    platform.Set("lua", DynValue.NewString(luaExecutable));
    platform.Set("compat", DynValue.NewBoolean(false));
    platform.Set("intsize", DynValue.NewNumber(IntPtr.Size));
    platform.Set("osname", DynValue.NewString(GetPlatformName()));
    script.Globals.Set("platform", DynValue.NewTable(platform));
}

static string GetLuaExecutableHint()
{
    string cliExecutablePath = ResolveCliExecutablePath();
    if (!string.IsNullOrEmpty(cliExecutablePath) && File.Exists(cliExecutablePath))
    {
        return $"dotnet \"{cliExecutablePath}\"";
    }

    return "lua";
}

static string ResolveCliExecutablePath()
{
    string baseDirectory = AppContext.BaseDirectory;

    string candidate = Path.Combine(
        baseDirectory,
        "..",
        "..",
        "..",
        "..",
        "..",
        "..",
        "src",
        "tooling",
        "NovaSharp.Cli",
        "bin",
        "Release",
        "net8.0",
        "NovaSharp.Cli.dll"
    );

    try
    {
        return Path.GetFullPath(candidate);
    }
    catch
    {
        return null;
    }
}

static string GetPlatformName()
{
    if (OperatingSystem.IsWindows())
    {
        return "MSWin32";
    }

    if (OperatingSystem.IsMacOS())
    {
        return "Darwin";
    }

    if (OperatingSystem.IsLinux())
    {
        return "Linux";
    }

    return "Unknown";
}
