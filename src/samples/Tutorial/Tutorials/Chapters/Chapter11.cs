using System;
using System.IO;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Loaders;
using NovaSharp.Interpreter.Modding;
using NovaSharp.Interpreter.Modules;
using NovaSharp.RemoteDebugger;

namespace Tutorials.Chapters
{
    [Tutorial]
    static class Chapter11
    {
        static RemoteDebuggerService remoteDebugger;

        private static Script AttachDebuggerScript(out string modDirectory)
        {
            modDirectory = LocateDebuggerModPath();
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                ScriptLoader = new FileSystemScriptLoader(),
            };

            if (remoteDebugger == null)
            {
                remoteDebugger = new RemoteDebuggerService();
            }

            Script script = remoteDebugger.AttachFromDirectory(
                modDirectory,
                "Description of the script",
                CoreModules.PresetComplete,
                options,
                infoSink: message => Console.WriteLine($"[compatibility] {message}"),
                warningSink: message => Console.WriteLine($"[compatibility] {message}")
            );

            string debuggerUrl = remoteDebugger.HttpUrlStringLocalHost;
            if (
                !string.IsNullOrWhiteSpace(debuggerUrl)
                && Uri.TryCreate(debuggerUrl, UriKind.Absolute, out Uri debuggerUri)
            )
            {
                // start the web browser at the correct URL. Replace this or just
                // pass the URL to the user in some other way.
                ProcessBrowserLauncher.Instance.Launch(debuggerUri);
            }

            return script;
        }

        private static string LocateDebuggerModPath()
        {
            string baseDirectory =
                AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            string relative = Path.Combine("Scripts", "DebuggerMod");
            string current = baseDirectory;

            while (!string.IsNullOrEmpty(current))
            {
                string candidate = Path.Combine(current, relative);
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                string parent = Path.GetDirectoryName(
                    current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                );
                if (string.IsNullOrEmpty(parent) || parent == current)
                {
                    break;
                }

                current = parent;
            }

            throw new InvalidOperationException(
                $"Unable to locate sample mod folder '{relative}'. Ensure the Tutorials project is executed from the repository tree so sample scripts are available."
            );
        }

        [Tutorial]
        static void DebuggerDemo()
        {
            Script script = AttachDebuggerScript(out string modDirectory);

            string entryPoint = Path.Combine(modDirectory, "main.lua");
            script.DoFile(entryPoint);

            Console.WriteLine("The script has ended..");
        }
    }
}
