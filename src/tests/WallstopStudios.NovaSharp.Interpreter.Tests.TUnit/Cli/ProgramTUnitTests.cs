namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Cli;
    using WallstopStudios.NovaSharp.Cli.Commands;
    using WallstopStudios.NovaSharp.Cli.Commands.Implementations;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.REPL;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [PlatformDetectorIsolation]
    public sealed class ProgramTUnitTests
    {
        private static readonly string[] HelpFlagArguments = { "-H" };
        private static readonly string[] ExecuteHelpCommandArguments = { "-X", "help" };
        private static readonly string[] ExecuteCommandMissingArgument = { "-X" };
        private static readonly string[] ExecuteUnknownCommandArguments = { "-X", "nope" };
        private static readonly string[] HardwireFlagArguments = { "-W" };
        private static readonly string[] InvalidLuaVersionArguments =
        {
            "--lua-version",
            "invalid",
            "script.lua",
        };
        private static readonly string[] LuaVersionMissingValueArguments = { "--lua-version" };

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
            using TempFileScope dumpFileScope = TempFileScope.Create(
                namePrefix: "dump_",
                extension: ".lua"
            );
            string dumpPath = dumpFileScope.FilePath;
            using TempFileScope destFileScope = TempFileScope.Create(
                namePrefix: "hardwire_",
                extension: ".vb"
            );
            string destPath = destFileScope.FilePath;

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
                    Script script = new(CoreModulePresets.Default);
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
            return new ShellContext(new Script(CoreModulePresets.Complete));
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
                : base(new Script(CoreModulePresets.Default)) { }

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

        #region TryParseLuaVersion Tests

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("5.1", LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments("5.2", LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments("5.3", LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments("5.4", LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments("5.5", LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments("51", LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments("52", LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments("53", LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments("54", LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments("55", LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments("Lua5.1", LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments("Lua5.2", LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments("Lua5.3", LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments("Lua5.4", LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments("Lua5.5", LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments("lua51", LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments("lua52", LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments("lua53", LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments("lua54", LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments("lua55", LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments("latest", LuaCompatibilityVersion.Latest)]
        [global::TUnit.Core.Arguments("Latest", LuaCompatibilityVersion.Latest)]
        [global::TUnit.Core.Arguments("LATEST", LuaCompatibilityVersion.Latest)]
        public async Task TryParseLuaVersionValidInputsReturnExpectedVersion(
            string input,
            LuaCompatibilityVersion expected
        )
        {
            bool success = Program.TryParseLuaVersion(input, out LuaCompatibilityVersion result);

            await Assert.That(success).IsTrue().ConfigureAwait(false);
            await Assert.That(result).IsEqualTo(expected).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(null)]
        [global::TUnit.Core.Arguments("")]
        [global::TUnit.Core.Arguments("   ")]
        [global::TUnit.Core.Arguments("5.0")]
        [global::TUnit.Core.Arguments("5.6")]
        [global::TUnit.Core.Arguments("invalid")]
        [global::TUnit.Core.Arguments("lua")]
        [global::TUnit.Core.Arguments("6.0")]
        [global::TUnit.Core.Arguments("abc")]
        public async Task TryParseLuaVersionInvalidInputsReturnFalse(string input)
        {
            bool success = Program.TryParseLuaVersion(input, out LuaCompatibilityVersion result);

            await Assert.That(success).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result)
                .IsEqualTo(LuaCompatibilityVersion.Latest)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsLuaVersionFlagSetsCompatibility()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope scriptScope = TempFileScope.Create(
                namePrefix: "version_test_",
                extension: ".lua"
            );
            string scriptPath = scriptScope.FilePath;
            await File.WriteAllTextAsync(scriptPath, "print(_VERSION)").ConfigureAwait(false);

            await WithConsoleAsync(async console =>
                {
                    // Note: Using short flag form
                    bool handled = Program.CheckArgs(
                        new[] { "-v", "5.2", scriptPath },
                        CreateShellContext()
                    );

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                    string output = console.Writer.ToString();
                    await Assert.That(output).Contains("Lua 5.2").ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsLuaVersionLongFlagSetsCompatibility()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope scriptScope = TempFileScope.Create(
                namePrefix: "version_test_",
                extension: ".lua"
            );
            string scriptPath = scriptScope.FilePath;
            await File.WriteAllTextAsync(scriptPath, "print(_VERSION)").ConfigureAwait(false);

            await WithConsoleAsync(async console =>
                {
                    // Note: Using long flag form
                    bool handled = Program.CheckArgs(
                        new[] { "--lua-version", "5.3", scriptPath },
                        CreateShellContext()
                    );

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                    string output = console.Writer.ToString();
                    await Assert.That(output).Contains("Lua 5.3").ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsInvalidLuaVersionReportsError()
        {
            await WithConsoleAsync(async console =>
                {
                    bool handled = Program.CheckArgs(
                        InvalidLuaVersionArguments,
                        CreateShellContext()
                    );

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                    string output = console.Writer.ToString();
                    await Assert.That(output).Contains("Invalid Lua version").ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckArgsLuaVersionWithMissingValueReportsError()
        {
            await WithConsoleAsync(async console =>
                {
                    bool handled = Program.CheckArgs(
                        LuaVersionMissingValueArguments,
                        CreateShellContext()
                    );

                    await Assert.That(handled).IsTrue().ConfigureAwait(false);
                    string output = console.Writer.ToString();
                    await Assert
                        .That(output)
                        .Contains(CliMessages.ProgramWrongSyntax)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        #endregion
    }
}
