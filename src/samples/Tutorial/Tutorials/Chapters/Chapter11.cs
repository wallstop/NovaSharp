using System;
using System.Diagnostics;
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

        static void ActivateRemoteDebugger(Script script)
        {
            if (remoteDebugger == null)
            {
                remoteDebugger = new RemoteDebuggerService();

                // the last boolean is to specify if the script is free to run
                // after attachment, defaults to false
                remoteDebugger.Attach(script, "Description of the script", false);
            }

            // start the web-browser at the correct url. Replace this or just
            // pass the url to the user in some way.
            Process.Start(remoteDebugger.HttpUrlStringLocalHost);
        }

        private static Script CreateDebuggerScript(out string modDirectory)
        {
            modDirectory = LocateDebuggerModPath();
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                ScriptLoader = new FileSystemScriptLoader(),
            };

            Script script = ModManifestCompatibility.CreateScriptFromDirectory(
                modDirectory,
                CoreModules.PresetComplete,
                options,
                infoSink: message => Console.WriteLine($"[compatibility] {message}"),
                warningSink: message => Console.WriteLine($"[compatibility] {message}")
            );

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
            Script script = CreateDebuggerScript(out string modDirectory);

            ActivateRemoteDebugger(script);

            string entryPoint = Path.Combine(modDirectory, "main.lua");
            script.DoFile(entryPoint);

            Console.WriteLine("The script has ended..");
        }
    }
}
