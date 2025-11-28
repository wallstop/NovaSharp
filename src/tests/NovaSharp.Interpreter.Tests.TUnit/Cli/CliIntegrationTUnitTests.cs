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

            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleRedirectionScope console = new("=1 + 1" + Environment.NewLine);
                Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));
                await Assert.That(console.Writer.ToString()).Contains("2");
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
            }
        }

        [global::TUnit.Core.Test]
        public async Task RunScriptArgumentPrintsSummaryAndExecutesFile()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            string scriptPath = Path.Combine(
                Path.GetTempPath(),
                $"cli-script-{Guid.NewGuid():N}.lua"
            );
            await File.WriteAllTextAsync(scriptPath, "print('cli integration sentinel')")
                .ConfigureAwait(false);

            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleRedirectionScope console = new();

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
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
                Cleanup(scriptPath);
                Cleanup(scriptPath + "-compiled");
            }
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsExecuteCommandRunsHelpCommand()
        {
            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleRedirectionScope console = new();

                bool handled = Program.CheckArgs(ExecuteHelpCommandArgs, CreateShellContext());

                await Assert.That(handled).IsTrue();
                await Assert
                    .That(console.Writer.ToString())
                    .Contains(CliMessages.HelpCommandCommandListHeading);
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
            }
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopExecutesBangCommandViaCommandManager()
        {
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);

            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleRedirectionScope console = new("!help" + Environment.NewLine);
                Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

                await Assert
                    .That(console.Writer.ToString())
                    .Contains(CliMessages.HelpCommandCommandListHeading);
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
            }
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopRegisterUnknownTypeReportsError()
        {
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);
            const string missingType = "NovaSharp.DoesNotExist.Sample";

            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleRedirectionScope console = new(
                    $"!register {missingType}{Environment.NewLine}"
                );

                Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

                await Assert
                    .That(console.Writer.ToString())
                    .Contains(CliMessages.RegisterCommandTypeNotFound(missingType));
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
            }
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

            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleRedirectionScope console = new(command + Environment.NewLine);

                Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

                await Assert.That(UserData.IsTypeRegistered(targetType)).IsTrue();
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
                UserData.UnregisterType(targetType);
            }
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopCompileCommandProducesChunk()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);

            string sourcePath = Path.Combine(
                Path.GetTempPath(),
                $"cli-compile-{Guid.NewGuid():N}.lua"
            );
            await File.WriteAllTextAsync(sourcePath, "return 42").ConfigureAwait(false);
            string compiledPath = sourcePath + "-compiled";

            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleRedirectionScope console = new(
                    $"!compile {sourcePath}{Environment.NewLine}"
                );

                Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));
                await Assert.That(File.Exists(compiledPath)).IsTrue();
                await Assert
                    .That(console.Writer.ToString())
                    .Contains(CliMessages.CompileCommandSuccess(compiledPath));
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
                Cleanup(sourcePath);
                Cleanup(compiledPath);
            }
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

            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleRedirectionScope console = new(
                    $"!compile {missingPath}{Environment.NewLine}"
                );

                Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

                await Assert
                    .That(console.Writer.ToString())
                    .Contains($"Failed to compile '{missingPath}'");
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
            }
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopRunCommandExecutesScript()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);
            string sourcePath = Path.Combine(Path.GetTempPath(), $"cli-run-{Guid.NewGuid():N}.lua");
            await File.WriteAllTextAsync(sourcePath, "print('run command sentinel')")
                .ConfigureAwait(false);

            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleRedirectionScope console = new(
                    $"!run {sourcePath}{Environment.NewLine}"
                );

                Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));
                await Assert.That(console.Writer.ToString()).Contains("run command sentinel");
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
                Cleanup(sourcePath);
            }
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

            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleRedirectionScope console = new("!debug" + Environment.NewLine);
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
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
                DebugCommand.DebuggerFactory = _originalDebuggerFactory;
                DebugCommand.BrowserLauncher = _originalBrowserLauncher;
            }
        }

        [global::TUnit.Core.Test]
        public async Task HardwireSwitchGeneratesDescriptorsViaProgramCheckArgs()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            string dumpPath = Path.Combine(
                Path.GetTempPath(),
                $"hardwire-dump-{Guid.NewGuid():N}.lua"
            );
            string destPath = Path.Combine(
                Path.GetTempPath(),
                $"hardwire-output-{Guid.NewGuid():N}.cs"
            );

            await HardwireDumpSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                Func<string, Table> originalLoader = HardwireCommand.DumpLoader;
                HardwireCommand.DumpLoader = _ =>
                {
                    Script script = new(default(CoreModules));
                    return HardwireTestUtilities.CreateDescriptorTable(script, "internal");
                };

                await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    using ConsoleRedirectionScope console = new();

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
                }
                finally
                {
                    ConsoleCaptureCoordinator.Semaphore.Release();
                    HardwireCommand.DumpLoader = originalLoader;
                    Cleanup(destPath);
                }
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
            string dumpPath = Path.Combine(
                Path.GetTempPath(),
                $"hardwire-repl-dump-{Guid.NewGuid():N}.lua"
            );
            string destPath = Path.Combine(
                Path.GetTempPath(),
                $"hardwire-repl-output-{Guid.NewGuid():N}.cs"
            );
            await File.WriteAllTextAsync(dumpPath, "-- placeholder").ConfigureAwait(false);

            await HardwireDumpSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                Func<string, Table> originalLoader = HardwireCommand.DumpLoader;
                HardwireCommand.DumpLoader = _ =>
                {
                    Script descriptorScript = new(default(CoreModules));
                    return HardwireTestUtilities.CreateDescriptorTable(descriptorScript, "public");
                };

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

                await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    using ConsoleRedirectionScope console = new(input);
                    Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

                    await Assert.That(File.Exists(destPath)).IsTrue();
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.HardwireGenerationSummary(0, 0));
                }
                finally
                {
                    ConsoleCaptureCoordinator.Semaphore.Release();
                    HardwireCommand.DumpLoader = originalLoader;
                    Cleanup(dumpPath);
                    Cleanup(destPath);
                }
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

        private static void Cleanup(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
