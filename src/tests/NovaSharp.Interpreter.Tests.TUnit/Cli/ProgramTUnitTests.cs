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
    using NovaSharp.Tests.TestInfrastructure.Scopes;

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
            await Assert.That(handled).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsHelpFlagWritesUsageAndReturnsTrue()
        {
            await WithConsoleAsync(async console =>
                {
                    bool handled = Program.CheckArgs(HelpFlagArguments, CreateShellContext());

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.ProgramUsageLong)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsExecuteCommandFlagRunsRequestedCommand()
        {
            await WithConsoleAsync(async console =>
                {
                    bool handled = Program.CheckArgs(
                        ExecuteHelpCommandArguments,
                        CreateShellContext()
                    );

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.HelpCommandCommandListHeading)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
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

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.ProgramWrongSyntax)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
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

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.CommandManagerInvalidCommand("nope"))
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsRunScriptExecutesFile()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope scriptScope = TempFileScope.Create(
                namePrefix: "sample_",
                extension: ".lua"
            );
            string scriptPath = scriptScope.FilePath;
            await File.WriteAllTextAsync(scriptPath, "return 42").ConfigureAwait(false);

            await WithConsoleAsync(async _ =>
                {
                    bool handled = Program.CheckArgs(new[] { scriptPath }, CreateShellContext());

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsRunScriptAppliesManifestCompatibility()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempDirectoryScope modDirectoryScope = TempDirectoryScope.Create(
                namePrefix: "mod_"
            );
            string modDirectory = modDirectoryScope.DirectoryPath;
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

            await WithConsoleAsync(async console =>
                {
                    bool handled = Program.CheckArgs(new[] { scriptPath }, CreateShellContext());

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(
                            CliMessages.ContextualCompatibilityInfo("Applied Lua 5.3 profile")
                        )
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsRunScriptLogsCompatibilitySummary()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempDirectoryScope modDirectoryScope = TempDirectoryScope.Create(
                namePrefix: "mod_"
            );
            string modDirectory = modDirectoryScope.DirectoryPath;
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

            await WithConsoleAsync(async console =>
                {
                    bool handled = Program.CheckArgs(new[] { scriptPath }, CreateShellContext());
                    string expectedSummary = GetCompatibilitySummary(LuaCompatibilityVersion.Lua52);
                    string expectedLine = CliMessages.ProgramRunningScript(
                        scriptPath,
                        expectedSummary
                    );

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(expectedLine)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsHardwireFlagWithMissingArgumentsShowsSyntax()
        {
            await WithConsoleAsync(async console =>
                {
                    bool handled = Program.CheckArgs(HardwireFlagArguments, CreateShellContext());

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.ProgramWrongSyntax)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsHardwireFlagGeneratesDescriptors()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            string dumpPath = Path.Combine(Path.GetTempPath(), $"dump_{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"hardwire_{Guid.NewGuid():N}.vb");
            using TempFileScope destFileScope = TempFileScope.FromExisting(destPath);

            using HardwireDumpLoaderScope dumpLoaderScope = HardwireDumpLoaderScope.Override(_ =>
            {
                Script script = new(default(CoreModules));
                return HardwireTestUtilities.CreateDescriptorTable(script, "internal");
            });

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

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.HardwireGenerationSummary(0, 0))
                        .ConfigureAwait(false);
                    await Assert.That(File.Exists(destPath)).IsTrue().ConfigureAwait(false);
                    string generated = await File.ReadAllTextAsync(destPath).ConfigureAwait(false);
                    await Assert
                        .That(generated)
                        .Contains("Namespace GeneratedNamespace")
                        .ConfigureAwait(false);
                    await Assert
                        .That(generated)
                        .Contains("Class GeneratedTypes")
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
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
                        await Assert
                            .That(interpreter.EvaluateCalled)
                            .IsFalse()
                            .ConfigureAwait(false);
                        await Assert
                            .That(output)
                            .Contains(CliMessages.HelpCommandCommandListHeading)
                            .ConfigureAwait(false);
                    },
                    "!help" + Environment.NewLine
                )
                .ConfigureAwait(false);
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
                        await Assert
                            .That(interpreter.EvaluateCalled)
                            .IsTrue()
                            .ConfigureAwait(false);
                        await Assert
                            .That(interpreter.LastInput)
                            .IsEqualTo("!help")
                            .ConfigureAwait(false);
                        await Assert.That(output).Contains("queued").ConfigureAwait(false);
                    },
                    "!help" + Environment.NewLine
                )
                .ConfigureAwait(false);
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
                        await Assert
                            .That(interpreter.EvaluateCalled)
                            .IsTrue()
                            .ConfigureAwait(false);
                        await Assert.That(output).DoesNotContain("Void").ConfigureAwait(false);
                    },
                    "return 1" + Environment.NewLine
                )
                .ConfigureAwait(false);
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
                        await Assert
                            .That(interpreter.EvaluateCalled)
                            .IsTrue()
                            .ConfigureAwait(false);
                        await Assert
                            .That(output)
                            .Contains("decorated message")
                            .ConfigureAwait(false);
                    },
                    "return 1" + Environment.NewLine
                )
                .ConfigureAwait(false);
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
                        await Assert
                            .That(interpreter.EvaluateCalled)
                            .IsTrue()
                            .ConfigureAwait(false);
                        await Assert.That(output).Contains("broken").ConfigureAwait(false);
                    },
                    "return 1" + Environment.NewLine
                )
                .ConfigureAwait(false);
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
                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(expectedActiveProfile)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
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

        private static Task WithConsoleAsync(
            Func<ConsoleRedirectionScope, Task> action,
            string input = null
        )
        {
            return ConsoleTestUtilities.WithConsoleRedirectionAsync(action, input);
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
