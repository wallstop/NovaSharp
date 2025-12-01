#pragma warning disable CA2007

namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [PlatformDetectorIsolation]
    public sealed class RunCommandTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ExecuteWithoutArgumentsWritesSyntaxHint()
        {
            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleCaptureScope consoleScope = new(captureError: false);
                RunCommand command = new();
                ShellContext context = new(new Script());

                command.Execute(context, string.Empty);

                await Assert.That(consoleScope.Writer.ToString()).Contains("Syntax : !run <file>");
            });
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteLoadsScriptThroughConfiguredLoader()
        {
            RecordingScriptLoader loader = new();
            Script script = new()
            {
                Globals = { ["marker"] = DynValue.NewNumber(0) },
                Options = { ScriptLoader = loader },
            };
            loader.ScriptBody = "marker = (marker or 0) + 1";

            RunCommand command = new();
            ShellContext context = new(script);

            command.Execute(context, "sample.lua");

            await Assert.That(loader.LastRequestedFile).IsEqualTo("sample.lua");
            await Assert.That(loader.LoadCount).IsEqualTo(1);
            await Assert.That(script.Globals.Get("marker").Number).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithMissingFilePropagatesLoaderException()
        {
            ThrowingScriptLoader loader = new();
            Script script = new() { Options = { ScriptLoader = loader } };
            RunCommand command = new();

            FileNotFoundException exception = ExpectException<FileNotFoundException>(() =>
                command.Execute(new ShellContext(script), "missing.lua")
            );

            await Assert.That(exception.Message).Contains("missing.lua");
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithoutManifestLogsCompatibilitySummary()
        {
            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                RecordingScriptLoader loader = new();
                Script script = new()
                {
                    Options =
                    {
                        ScriptLoader = loader,
                        CompatibilityVersion = LuaCompatibilityVersion.Lua54,
                    },
                };
                RunCommand command = new();
                ShellContext context = new(script);

                using ConsoleCaptureScope consoleScope = new(captureError: false);

                command.Execute(context, "sample.lua");

                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains("[compatibility] Running")
                    .And.Contains("Lua 5.4");
            });
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithManifestRunsScriptInCompatibilityInstance()
        {
            using TempDirectoryScope modDirectoryScope = TempDirectoryScope.Create(
                namePrefix: "mod_"
            );
            string modDirectory = modDirectoryScope.DirectoryPath;

            string scriptPath = Path.Combine(modDirectory, "entry.lua");
            await File.WriteAllTextAsync(
                    scriptPath,
                    "if warn ~= nil then error('warn available') end\ncontextFlag = true\n"
                )
                .ConfigureAwait(false);

            string manifestPath = Path.Combine(modDirectory, "mod.json");
            await File.WriteAllTextAsync(
                    manifestPath,
                    "{\n"
                        + "    \"name\": \"CompatMod\",\n"
                        + "    \"luaCompatibility\": \"Lua53\"\n"
                        + "}\n"
                )
                .ConfigureAwait(false);

            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                RunCommand command = new();
                Script script = new();
                ShellContext context = new(script);

                using ConsoleCaptureScope consoleScope = new(captureError: false);

                command.Execute(context, scriptPath);

                string consoleOutput = consoleScope.Writer.ToString();
                await Assert
                    .That(consoleOutput)
                    .Contains("[compatibility] Applied Lua 5.3 profile")
                    .And.Contains("Lua 5.3")
                    .And.Contains("[compatibility] Running");
                await Assert.That(script.Globals.Get("contextFlag").IsNil()).IsTrue();
            });
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }

        private sealed class RecordingScriptLoader : IScriptLoader
        {
            public string ScriptBody { get; set; } = "return";

            public string LastRequestedFile { get; private set; } = string.Empty;

            public int LoadCount { get; private set; }

            public object LoadFile(string file, Table globalContext)
            {
                LastRequestedFile = file;
                LoadCount++;
                return ScriptBody;
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return modname;
            }
        }

        private sealed class ThrowingScriptLoader : IScriptLoader
        {
            public object LoadFile(string file, Table globalContext)
            {
                throw new FileNotFoundException($"Missing {file}");
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return modname;
            }
        }
    }
}

#pragma warning restore CA2007
