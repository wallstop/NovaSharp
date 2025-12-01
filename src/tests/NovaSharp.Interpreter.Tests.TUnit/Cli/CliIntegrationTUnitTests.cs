#pragma warning disable CA2007

namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.REPL;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using NovaSharp.Interpreter.Tests.Units;
    using NovaSharp.RemoteDebugger;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [PlatformDetectorIsolation]
    [UserDataIsolation]
    public sealed class CliIntegrationTUnitTests : IDisposable
    {
        private static readonly string[] ExecuteHelpCommandArgs = { "-X", "help" };
        private static readonly SemaphoreSlim HardwireDumpSemaphore = new(1, 1);
        private readonly Func<IRemoteDebuggerBridge> _originalDebuggerFactory;
        private readonly IBrowserLauncher _originalBrowserLauncher;

        static CliIntegrationTUnitTests()
        {
            if (CommandManager.Find("help") == null)
            {
                CommandManager.Initialize();
            }
        }

        public CliIntegrationTUnitTests()
        {
            _originalDebuggerFactory = DebugCommand.DebuggerFactory;
            _originalBrowserLauncher = DebugCommand.BrowserLauncher;
        }

        public void Dispose()
        {
            DebugCommand.DebuggerFactory = _originalDebuggerFactory;
            DebugCommand.BrowserLauncher = _originalBrowserLauncher;
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopEvaluatesLuaInputAndPrintsResult()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script) { HandleClassicExprsSyntax = true };

            await WithConsoleAsync(
                async console =>
                {
                    Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));
                    await Assert.That(console.Writer.ToString()).Contains("2");
                },
                "=1 + 1" + Environment.NewLine
            );
        }

        [global::TUnit.Core.Test]
        public async Task RunScriptArgumentPrintsSummaryAndExecutesFile()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope scriptScope = TempFileScope.Create(
                namePrefix: "cli-script-",
                extension: ".lua"
            );
            string scriptPath = scriptScope.FilePath;
            using TempFileScope compiledScope = TempFileScope.FromExisting(
                scriptPath + "-compiled"
            );
            await File.WriteAllTextAsync(scriptPath, "print('cli integration sentinel')")
                .ConfigureAwait(false);

            await WithConsoleAsync(async console =>
            {
                bool handled = Program.CheckArgs(new[] { scriptPath }, CreateShellContext());
                string resolvedPath = Path.GetFullPath(scriptPath);

                ScriptOptions expectedOptions = new(Script.DefaultOptions);
                Script summaryScript = new(CoreModules.PresetComplete, expectedOptions);
                string expectedSummary = summaryScript.CompatibilityProfile.GetFeatureSummary();
                string expectedBanner = CliMessages.ProgramRunningScript(
                    resolvedPath,
                    expectedSummary
                );

                await Assert.That(handled).IsTrue();
                string output = console.Writer.ToString();
                await Assert.That(output).Contains(expectedBanner);
                await Assert.That(output).Contains("cli integration sentinel");
            });
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsExecuteCommandRunsHelpCommand()
        {
            await WithConsoleAsync(async console =>
            {
                bool handled = Program.CheckArgs(ExecuteHelpCommandArgs, CreateShellContext());

                await Assert.That(handled).IsTrue();
                await Assert
                    .That(console.Writer.ToString())
                    .Contains(CliMessages.HelpCommandCommandListHeading);
            });
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopExecutesBangCommandViaCommandManager()
        {
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);

            await WithConsoleAsync(
                async console =>
                {
                    Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.HelpCommandCommandListHeading);
                },
                "!help" + Environment.NewLine
            );
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopRegisterUnknownTypeReportsError()
        {
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);
            const string missingType = "NovaSharp.DoesNotExist.Sample";

            await WithConsoleAsync(
                async console =>
                {
                    Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.RegisterCommandTypeNotFound(missingType));
                },
                $"!register {missingType}{Environment.NewLine}"
            );
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopRegisterKnownTypeRegistersUserData()
        {
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);
            TestRegistrationType instance = new();
            Type targetType = instance.GetType();
            string command = $"!register {targetType.AssemblyQualifiedName}";

            if (UserData.IsTypeRegistered(targetType))
            {
                UserData.UnregisterType(targetType);
            }

            try
            {
                await WithConsoleAsync(
                    async console =>
                    {
                        Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

                        await Assert.That(UserData.IsTypeRegistered(targetType)).IsTrue();
                    },
                    command + Environment.NewLine
                );
            }
            finally
            {
                UserData.UnregisterType(targetType);
            }
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopCompileCommandProducesChunk()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);

            using TempFileScope sourceScope = TempFileScope.Create(
                namePrefix: "cli-compile-",
                extension: ".lua"
            );
            string sourcePath = sourceScope.FilePath;
            await File.WriteAllTextAsync(sourcePath, "return 42").ConfigureAwait(false);
            string compiledPath = sourcePath + "-compiled";
            using TempFileScope compiledScope = TempFileScope.FromExisting(compiledPath);

            await WithConsoleAsync(
                async console =>
                {
                    Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));
                    await Assert.That(File.Exists(compiledPath)).IsTrue();
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.CompileCommandSuccess(compiledPath));
                },
                $"!compile {sourcePath}{Environment.NewLine}"
            );
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopCompileCommandWithMissingFilePrintsError()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);
            string missingPath = Path.Combine(
                Path.GetTempPath(),
                $"cli-missing-{Guid.NewGuid():N}.lua"
            );

            await WithConsoleAsync(
                async console =>
                {
                    Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

                    await Assert
                        .That(console.Writer.ToString())
                        .Contains($"Failed to compile '{missingPath}'");
                },
                $"!compile {missingPath}{Environment.NewLine}"
            );
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopRunCommandExecutesScript()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);
            using TempFileScope sourceScope = TempFileScope.Create(
                namePrefix: "cli-run-",
                extension: ".lua"
            );
            string sourcePath = sourceScope.FilePath;
            await File.WriteAllTextAsync(sourcePath, "print('run command sentinel')")
                .ConfigureAwait(false);

            await WithConsoleAsync(
                async console =>
                {
                    Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));
                    await Assert.That(console.Writer.ToString()).Contains("run command sentinel");
                },
                $"!run {sourcePath}{Environment.NewLine}"
            );
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopDebugCommandUsesInjectedDebugger()
        {
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);
            TestDebuggerBridge bridge = new();
            TestBrowserLauncher launcher = new()
            {
                UrlToReturn = new Uri("http://localhost:1234/"),
            };

            DebugCommand.DebuggerFactory = () => bridge;
            DebugCommand.BrowserLauncher = launcher;

            try
            {
                await WithConsoleAsync(
                    async console =>
                    {
                        Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

                        string compatibilityLine = script.CompatibilityProfile.GetFeatureSummary();
                        await Assert.That(bridge.AttachCount).IsEqualTo(1);
                        await Assert.That(bridge.LastScript).IsSameReferenceAs(script);
                        await Assert.That(launcher.LaunchCount).IsEqualTo(1);
                        await Assert
                            .That(console.Writer.ToString())
                            .Contains(
                                $"[compatibility] Debugger session running under {compatibilityLine}"
                            );
                    },
                    "!debug" + Environment.NewLine
                );
            }
            finally
            {
                DebugCommand.DebuggerFactory = _originalDebuggerFactory;
                DebugCommand.BrowserLauncher = _originalBrowserLauncher;
            }
        }

        [global::TUnit.Core.Test]
        public async Task HardwireSwitchGeneratesDescriptorsViaProgramCheckArgs()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope dumpScope = TempFileScope.Create(
                namePrefix: "hardwire-dump-",
                extension: ".lua"
            );
            string dumpPath = dumpScope.FilePath;
            string destPath = Path.Combine(
                Path.GetTempPath(),
                $"hardwire-output-{Guid.NewGuid():N}.cs"
            );
            using TempFileScope destScope = TempFileScope.FromExisting(destPath);

            await HardwireDumpSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using HardwireDumpLoaderScope dumpLoaderScope = HardwireDumpLoaderScope.Override(
                    _ =>
                    {
                        Script script = new(default(CoreModules));
                        return HardwireTestUtilities.CreateDescriptorTable(script, "internal");
                    }
                );

                await WithConsoleAsync(async console =>
                {
                    string[] args =
                    {
                        "-W",
                        dumpPath,
                        destPath,
                        "--class:GeneratedTypes",
                        "--namespace:GeneratedNamespace",
                        "--internals",
                    };

                    bool handled = Program.CheckArgs(args, CreateShellContext());
                    await Assert.That(handled).IsTrue();
                    await Assert.That(File.Exists(destPath)).IsTrue();
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.HardwireGenerationSummary(0, 0));
                });
            }
            finally
            {
                HardwireDumpSemaphore.Release();
            }
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopHardwireCommandGeneratesDescriptors()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);
            using TempFileScope dumpScope = TempFileScope.Create(
                namePrefix: "hardwire-repl-dump-",
                extension: ".lua"
            );
            string dumpPath = dumpScope.FilePath;
            string destPath = Path.Combine(
                Path.GetTempPath(),
                $"hardwire-repl-output-{Guid.NewGuid():N}.cs"
            );
            using TempFileScope destScope = TempFileScope.FromExisting(destPath);
            await File.WriteAllTextAsync(dumpPath, "-- placeholder").ConfigureAwait(false);

            await HardwireDumpSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using HardwireDumpLoaderScope dumpLoaderScope = HardwireDumpLoaderScope.Override(
                    _ =>
                    {
                        Script descriptorScript = new(default(CoreModules));
                        return HardwireTestUtilities.CreateDescriptorTable(
                            descriptorScript,
                            "public"
                        );
                    }
                );

                string input =
                    string.Join(
                        Environment.NewLine,
                        "!hardwire",
                        string.Empty,
                        dumpPath,
                        destPath,
                        "y",
                        "GeneratedNamespace",
                        "GeneratedTypes"
                    ) + Environment.NewLine;

                await WithConsoleAsync(
                    async console =>
                    {
                        Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

                        await Assert.That(File.Exists(destPath)).IsTrue();
                        await Assert
                            .That(console.Writer.ToString())
                            .Contains(CliMessages.HardwireGenerationSummary(0, 0));
                    },
                    input
                );
            }
            finally
            {
                HardwireDumpSemaphore.Release();
            }
        }

        private static ShellContext CreateShellContext()
        {
            return new ShellContext(new Script());
        }

        private static Task WithConsoleAsync(
            Func<ConsoleRedirectionScope, Task> action,
            string input = null
        )
        {
            return ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleRedirectionScope console = new(input);
                await action(console).ConfigureAwait(false);
            });
        }

        private sealed class TestDebuggerBridge : IRemoteDebuggerBridge
        {
            public int AttachCount { get; private set; }

            public Script LastScript { get; private set; }

            public string LastName { get; private set; } = string.Empty;

            public bool FreeRunRequested { get; private set; }

            public Uri HttpUrlStringLocalHost { get; set; } = new Uri("http://localhost/");

            public void Attach(Script script, string scriptName, bool freeRunAfterAttach)
            {
                AttachCount++;
                LastScript = script;
                LastName = scriptName;
                FreeRunRequested = freeRunAfterAttach;
            }
        }

        private sealed class TestBrowserLauncher : IBrowserLauncher
        {
            public int LaunchCount { get; private set; }

            public Uri LastUrl { get; private set; }

            public Uri UrlToReturn { get; set; } = new Uri("http://localhost/");

            public void Launch(Uri url)
            {
                LaunchCount++;
                LastUrl = url;
            }
        }

        private sealed class TestRegistrationType { }
    }
}

#pragma warning restore CA2007
