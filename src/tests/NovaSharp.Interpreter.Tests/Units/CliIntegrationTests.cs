namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.REPL;
    using NovaSharp.Interpreter.Tests.Utilities;
    using NovaSharp.RemoteDebugger;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CliIntegrationTests
    {
        private static readonly string[] ExecuteHelpCommandArgs = { "-X", "help" };
        private Func<IRemoteDebuggerBridge> _originalDebuggerFactory = null!;
        private IBrowserLauncher _originalBrowserLauncher = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (CommandManager.Find("help") == null)
            {
                CommandManager.Initialize();
            }

            _originalDebuggerFactory = DebugCommand.DebuggerFactory;
            _originalBrowserLauncher = DebugCommand.BrowserLauncher;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            DebugCommand.DebuggerFactory = _originalDebuggerFactory;
            DebugCommand.BrowserLauncher = _originalBrowserLauncher;
        }

        [Test]
        public void InterpreterLoopEvaluatesLuaInputAndPrintsResult()
        {
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script) { HandleClassicExprsSyntax = true };

            using ConsoleRedirectionScope console = new("=1 + 1" + Environment.NewLine);

            Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

            Assert.That(console.Writer.ToString(), Does.Contain("2"));
        }

        [Test]
        public void RunScriptArgumentPrintsSummaryAndExecutesFile()
        {
            string scriptPath = Path.Combine(
                Path.GetTempPath(),
                $"cli-script-{Guid.NewGuid():N}.lua"
            );
            File.WriteAllText(scriptPath, "print('cli integration sentinel')");

            using ConsoleRedirectionScope console = new();

            try
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

                Assert.Multiple(() =>
                {
                    Assert.That(handled, Is.True);
                    string output = console.Writer.ToString();
                    Assert.That(output, Does.Contain(expectedBanner));
                    Assert.That(output, Does.Contain("cli integration sentinel"));
                });
            }
            finally
            {
                if (File.Exists(scriptPath))
                {
                    File.Delete(scriptPath);
                }
            }
        }

        [Test]
        public void CheckArgsExecuteCommandRunsHelpCommand()
        {
            using ConsoleRedirectionScope console = new();

            bool handled = Program.CheckArgs(ExecuteHelpCommandArgs, CreateShellContext());

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(
                    console.Writer.ToString(),
                    Does.Contain(CliMessages.HelpCommandCommandListHeading)
                );
            });
        }

        [Test]
        public void InterpreterLoopExecutesBangCommandViaCommandManager()
        {
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);

            using ConsoleRedirectionScope console = new("!help" + Environment.NewLine);

            Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

            Assert.That(
                console.Writer.ToString(),
                Does.Contain(CliMessages.HelpCommandCommandListHeading)
            );
        }

        [Test]
        public void InterpreterLoopRegisterUnknownTypeReportsError()
        {
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);
            const string missingType = "NovaSharp.DoesNotExist.Sample";

            using ConsoleRedirectionScope console = new(
                $"!register {missingType}{Environment.NewLine}"
            );

            Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

            Assert.That(
                console.Writer.ToString(),
                Does.Contain(CliMessages.RegisterCommandTypeNotFound(missingType))
            );
        }

        [Test]
        public void InterpreterLoopRegisterKnownTypeRegistersUserData()
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

            using ConsoleRedirectionScope console = new(command + Environment.NewLine);

            Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

            try
            {
                Assert.That(UserData.IsTypeRegistered(targetType), Is.True);
            }
            finally
            {
                UserData.UnregisterType(targetType);
            }
        }

        [Test]
        public void InterpreterLoopCompileCommandProducesChunk()
        {
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);

            string sourcePath = Path.Combine(
                Path.GetTempPath(),
                $"cli-compile-{Guid.NewGuid():N}.lua"
            );
            File.WriteAllText(sourcePath, "return 42");
            string compiledPath = sourcePath + "-compiled";

            using ConsoleRedirectionScope console = new(
                $"!compile {sourcePath}{Environment.NewLine}"
            );

            try
            {
                Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));
                Assert.That(File.Exists(compiledPath), Is.True);
                Assert.That(
                    console.Writer.ToString(),
                    Does.Contain(CliMessages.CompileCommandSuccess(compiledPath))
                );
            }
            finally
            {
                if (File.Exists(sourcePath))
                {
                    File.Delete(sourcePath);
                }

                if (File.Exists(compiledPath))
                {
                    File.Delete(compiledPath);
                }
            }
        }

        [Test]
        public void InterpreterLoopCompileCommandWithMissingFilePrintsError()
        {
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);
            string missingPath = Path.Combine(
                Path.GetTempPath(),
                $"cli-missing-{Guid.NewGuid():N}.lua"
            );

            using ConsoleRedirectionScope console = new(
                $"!compile {missingPath}{Environment.NewLine}"
            );

            Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));

            Assert.That(
                console.Writer.ToString(),
                Does.Contain($"Failed to compile '{missingPath}'")
            );
        }

        [Test]
        public void InterpreterLoopRunCommandExecutesScript()
        {
            Script script = new(CoreModules.PresetComplete);
            ReplInterpreter interpreter = new(script);
            string sourcePath = Path.Combine(Path.GetTempPath(), $"cli-run-{Guid.NewGuid():N}.lua");
            File.WriteAllText(sourcePath, "print('run command sentinel')");

            using ConsoleRedirectionScope console = new($"!run {sourcePath}{Environment.NewLine}");

            try
            {
                Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));
                Assert.That(console.Writer.ToString(), Does.Contain("run command sentinel"));
            }
            finally
            {
                if (File.Exists(sourcePath))
                {
                    File.Delete(sourcePath);
                }
            }
        }

        [Test]
        public void InterpreterLoopDebugCommandUsesInjectedDebugger()
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

            using ConsoleRedirectionScope console = new("!debug" + Environment.NewLine);

            try
            {
                Program.RunInterpreterLoopForTests(interpreter, new ShellContext(script));
            }
            finally
            {
                DebugCommand.DebuggerFactory = _originalDebuggerFactory;
                DebugCommand.BrowserLauncher = _originalBrowserLauncher;
            }

            string compatibilityLine = script.CompatibilityProfile.GetFeatureSummary();
            Assert.Multiple(() =>
            {
                Assert.That(bridge.AttachCount, Is.EqualTo(1));
                Assert.That(bridge.LastScript, Is.SameAs(script));
                Assert.That(launcher.LaunchCount, Is.EqualTo(1));
                Assert.That(
                    console.Writer.ToString(),
                    Does.Contain(
                        $"[compatibility] Debugger session running under {compatibilityLine}"
                    )
                );
            });
        }

        [Test]
        public void HardwireSwitchGeneratesDescriptorsViaProgramCheckArgs()
        {
            string dumpPath = Path.Combine(
                Path.GetTempPath(),
                $"hardwire-dump-{Guid.NewGuid():N}.lua"
            );
            string destPath = Path.Combine(
                Path.GetTempPath(),
                $"hardwire-output-{Guid.NewGuid():N}.cs"
            );

            Func<string, Table> originalLoader = HardwireCommand.DumpLoader;
            HardwireCommand.DumpLoader = _ =>
            {
                Script script = new(default(CoreModules));
                return HardwireTestUtilities.CreateDescriptorTable(script, "internal");
            };

            using ConsoleRedirectionScope console = new();

            try
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
                Assert.Multiple(() =>
                {
                    Assert.That(handled, Is.True);
                    Assert.That(File.Exists(destPath), Is.True);
                    Assert.That(
                        console.Writer.ToString(),
                        Does.Contain(CliMessages.HardwireGenerationSummary(0, 0))
                    );
                });
            }
            finally
            {
                HardwireCommand.DumpLoader = originalLoader;
                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }
            }
        }

        private static ShellContext CreateShellContext()
        {
            return new ShellContext(new Script());
        }

        private sealed class TestDebuggerBridge : IRemoteDebuggerBridge
        {
            public int AttachCount { get; private set; }

            public Script LastScript { get; private set; } = null!;

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

            public Uri LastUrl { get; private set; } = null!;

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
