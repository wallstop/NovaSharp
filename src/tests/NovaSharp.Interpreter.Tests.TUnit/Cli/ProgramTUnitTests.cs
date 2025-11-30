#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.REPL;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using NovaSharp.Interpreter.Tests.Units;

    [PlatformDetectorIsolation]
    public sealed class ProgramTUnitTests
    {
        private static readonly string[] HelpFlagArguments = { "-H" };
        private static readonly string[] ExecuteHelpCommandArguments = { "-X", "help" };
        private static readonly string[] ExecuteCommandMissingArgument = { "-X" };
        private static readonly string[] ExecuteUnknownCommandArguments = { "-X", "nope" };
        private static readonly string[] HardwireFlagArguments = { "-W" };

        static ProgramTUnitTests()
        {
            if (CommandManager.Find("help") == null)
            {
                CommandManager.Initialize();
            }
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsNoArgumentsReturnsFalse()
        {
            bool handled = Program.CheckArgs(Array.Empty<string>(), CreateShellContext());
            await Assert.That(handled).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsHelpFlagWritesUsageAndReturnsTrue()
        {
            await WithConsoleAsync(async console =>
            {
                bool handled = Program.CheckArgs(HelpFlagArguments, CreateShellContext());

                await Assert.That(handled).IsTrue();
                await Assert.That(console.Writer.ToString()).Contains(CliMessages.ProgramUsageLong);
            });
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsExecuteCommandFlagRunsRequestedCommand()
        {
            await WithConsoleAsync(async console =>
            {
                bool handled = Program.CheckArgs(ExecuteHelpCommandArguments, CreateShellContext());

                await Assert.That(handled).IsTrue();
                await Assert
                    .That(console.Writer.ToString())
                    .Contains(CliMessages.HelpCommandCommandListHeading);
            });
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsExecuteCommandFlagWithMissingArgumentShowsSyntax()
        {
            await WithConsoleAsync(async console =>
            {
                bool handled = Program.CheckArgs(
                    ExecuteCommandMissingArgument,
                    CreateShellContext()
                );

                await Assert.That(handled).IsTrue();
                await Assert
                    .That(console.Writer.ToString())
                    .Contains(CliMessages.ProgramWrongSyntax);
            });
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsExecuteCommandFlagWithUnknownCommandReportsError()
        {
            await WithConsoleAsync(async console =>
            {
                bool handled = Program.CheckArgs(
                    ExecuteUnknownCommandArguments,
                    CreateShellContext()
                );

                await Assert.That(handled).IsTrue();
                await Assert
                    .That(console.Writer.ToString())
                    .Contains(CliMessages.CommandManagerInvalidCommand("nope"));
            });
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsRunScriptExecutesFile()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            string scriptPath = Path.Combine(Path.GetTempPath(), $"sample_{Guid.NewGuid():N}.lua");
            await File.WriteAllTextAsync(scriptPath, "return 42").ConfigureAwait(false);

            try
            {
                await WithConsoleAsync(async _ =>
                {
                    bool handled = Program.CheckArgs(new[] { scriptPath }, CreateShellContext());

                    await Assert.That(handled).IsTrue();
                });
            }
            finally
            {
                DeleteFileIfExists(scriptPath);
            }
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsRunScriptAppliesManifestCompatibility()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            string modDirectory = Path.Combine(Path.GetTempPath(), $"mod_{Guid.NewGuid():N}");
            Directory.CreateDirectory(modDirectory);
            string scriptPath = Path.Combine(modDirectory, "entry.lua");
            string manifestPath = Path.Combine(modDirectory, "mod.json");

            await File.WriteAllTextAsync(
                    scriptPath,
                    "if warn ~= nil then error('warn available') end"
                )
                .ConfigureAwait(false);
            await File.WriteAllTextAsync(
                    manifestPath,
                    "{\n"
                        + "    \"name\": \"CompatMod\",\n"
                        + "    \"luaCompatibility\": \"Lua53\"\n"
                        + "}\n"
                )
                .ConfigureAwait(false);

            try
            {
                await WithConsoleAsync(async console =>
                {
                    bool handled = Program.CheckArgs(new[] { scriptPath }, CreateShellContext());

                    await Assert.That(handled).IsTrue();
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(
                            CliMessages.ContextualCompatibilityInfo("Applied Lua 5.3 profile")
                        );
                });
            }
            finally
            {
                DeleteDirectoryIfExists(modDirectory);
            }
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsRunScriptLogsCompatibilitySummary()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            string modDirectory = Path.Combine(Path.GetTempPath(), $"mod_{Guid.NewGuid():N}");
            Directory.CreateDirectory(modDirectory);
            string scriptPath = Path.Combine(modDirectory, "entry.lua");
            string manifestPath = Path.Combine(modDirectory, "mod.json");

            await File.WriteAllTextAsync(scriptPath, "return 0").ConfigureAwait(false);
            await File.WriteAllTextAsync(
                    manifestPath,
                    "{\n"
                        + "    \"name\": \"CompatMod\",\n"
                        + "    \"luaCompatibility\": \"Lua52\"\n"
                        + "}\n"
                )
                .ConfigureAwait(false);

            try
            {
                await WithConsoleAsync(async console =>
                {
                    bool handled = Program.CheckArgs(new[] { scriptPath }, CreateShellContext());
                    string expectedSummary = GetCompatibilitySummary(LuaCompatibilityVersion.Lua52);
                    string expectedLine = CliMessages.ProgramRunningScript(
                        scriptPath,
                        expectedSummary
                    );

                    await Assert.That(handled).IsTrue();
                    await Assert.That(console.Writer.ToString()).Contains(expectedLine);
                });
            }
            finally
            {
                DeleteDirectoryIfExists(modDirectory);
            }
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsHardwireFlagWithMissingArgumentsShowsSyntax()
        {
            await WithConsoleAsync(async console =>
            {
                bool handled = Program.CheckArgs(HardwireFlagArguments, CreateShellContext());

                await Assert.That(handled).IsTrue();
                await Assert
                    .That(console.Writer.ToString())
                    .Contains(CliMessages.ProgramWrongSyntax);
            });
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsHardwireFlagGeneratesDescriptors()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            string dumpPath = Path.Combine(Path.GetTempPath(), $"dump_{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"hardwire_{Guid.NewGuid():N}.vb");

            Func<string, Table> originalLoader = HardwireCommand.DumpLoader;
            HardwireCommand.DumpLoader = _ =>
            {
                Script script = new(default(CoreModules));
                return HardwireTestUtilities.CreateDescriptorTable(script, "internal");
            };

            try
            {
                await WithConsoleAsync(async console =>
                {
                    bool handled = Program.CheckArgs(
                        new[]
                        {
                            "-W",
                            dumpPath,
                            destPath,
                            "--internals",
                            "--vb",
                            "--class:GeneratedTypes",
                            "--namespace:GeneratedNamespace",
                        },
                        CreateShellContext()
                    );

                    await Assert.That(handled).IsTrue();
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.HardwireGenerationSummary(0, 0));
                    await Assert.That(File.Exists(destPath)).IsTrue();
                    string generated = await File.ReadAllTextAsync(destPath).ConfigureAwait(false);
                    await Assert.That(generated).Contains("Namespace GeneratedNamespace");
                    await Assert.That(generated).Contains("Class GeneratedTypes");
                });
            }
            finally
            {
                HardwireCommand.DumpLoader = originalLoader;
                DeleteFileIfExists(destPath);
            }
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopExecutesCommandsWhenNoPendingInput()
        {
            StubReplInterpreter interpreter = new();

            await WithConsoleAsync(
                async console =>
                {
                    RunInterpreterLoop(interpreter);

                    string output = console.Writer.ToString();
                    await Assert.That(interpreter.EvaluateCalled).IsFalse();
                    await Assert.That(output).Contains(CliMessages.HelpCommandCommandListHeading);
                },
                "!help" + Environment.NewLine
            );
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopEvaluatesInputWhenCommandIsPending()
        {
            StubReplInterpreter interpreter = new()
            {
                PendingCommand = true,
                ReturnValue = DynValue.NewString("queued"),
            };

            await WithConsoleAsync(
                async console =>
                {
                    RunInterpreterLoop(interpreter);

                    string output = console.Writer.ToString();
                    await Assert.That(interpreter.EvaluateCalled).IsTrue();
                    await Assert.That(interpreter.LastInput).IsEqualTo("!help");
                    await Assert.That(output).Contains("queued");
                },
                "!help" + Environment.NewLine
            );
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopDoesNotPrintVoidResults()
        {
            StubReplInterpreter interpreter = new() { ReturnValue = DynValue.Void };

            await WithConsoleAsync(
                async console =>
                {
                    RunInterpreterLoop(interpreter);

                    string output = console.Writer.ToString();
                    await Assert.That(interpreter.EvaluateCalled).IsTrue();
                    await Assert.That(output).DoesNotContain("Void");
                },
                "return 1" + Environment.NewLine
            );
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopPrintsInterpreterExceptionDecoratedMessage()
        {
            StubReplInterpreter interpreter = new()
            {
                ExceptionToThrow = new ScriptRuntimeException("boom")
                {
                    DecoratedMessage = "decorated message",
                },
            };

            await WithConsoleAsync(
                async console =>
                {
                    RunInterpreterLoop(interpreter);

                    string output = console.Writer.ToString();
                    await Assert.That(interpreter.EvaluateCalled).IsTrue();
                    await Assert.That(output).Contains("decorated message");
                },
                "return 1" + Environment.NewLine
            );
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterLoopPrintsGeneralExceptionMessage()
        {
            StubReplInterpreter interpreter = new()
            {
                ExceptionToThrow = new InvalidOperationException("broken"),
            };

            await WithConsoleAsync(
                async console =>
                {
                    RunInterpreterLoop(interpreter);

                    string output = console.Writer.ToString();
                    await Assert.That(interpreter.EvaluateCalled).IsTrue();
                    await Assert.That(output).Contains("broken");
                },
                "return 1" + Environment.NewLine
            );
        }

        [global::TUnit.Core.Test]
        public async Task BannerPrintsCompatibilitySummary()
        {
            await WithConsoleAsync(async console =>
            {
                Script script = new(CoreModules.PresetDefault);
                script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua53;

                Program.ShowBannerForTests(script);

                string expectedActiveProfile = CliMessages.ProgramActiveProfile(
                    script.CompatibilityProfile.GetFeatureSummary()
                );
                await Assert.That(console.Writer.ToString()).Contains(expectedActiveProfile);
            });
        }

        private static string GetCompatibilitySummary(LuaCompatibilityVersion version)
        {
            Script script = new(new ScriptOptions { CompatibilityVersion = version });
            return script.CompatibilityProfile.GetFeatureSummary();
        }

        private static ShellContext CreateShellContext()
        {
            return new ShellContext(new Script());
        }

        private static void RunInterpreterLoop(ReplInterpreter interpreter)
        {
            Program.RunInterpreterLoopForTests(interpreter, CreateShellContext());
        }

        private static void DeleteFileIfExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        private static async Task WithConsoleAsync(
            Func<ConsoleRedirectionScope, Task> action,
            string input = null
        )
        {
            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleRedirectionScope console = new(input);
                await action(console).ConfigureAwait(false);
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
            }
        }

        private sealed class StubReplInterpreter : ReplInterpreter
        {
            public StubReplInterpreter()
                : base(new Script(CoreModules.PresetDefault)) { }

            public bool PendingCommand { get; set; }

            public bool EvaluateCalled { get; private set; }

            public string LastInput { get; private set; } = string.Empty;

            public DynValue ReturnValue { get; set; } = DynValue.NewNumber(1);

            public Exception ExceptionToThrow { get; set; }

            public override bool HasPendingCommand
            {
                get { return PendingCommand; }
            }

            public override string ClassicPrompt
            {
                get { return ">"; }
            }

            public override DynValue Evaluate(string input)
            {
                EvaluateCalled = true;
                LastInput = input;

                if (ExceptionToThrow != null)
                {
                    throw ExceptionToThrow;
                }

                return ReturnValue;
            }
        }
    }
}
#pragma warning restore CA2007
