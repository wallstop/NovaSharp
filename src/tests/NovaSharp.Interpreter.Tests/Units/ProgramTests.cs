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
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.REPL;
    using NovaSharp.Interpreter.Tests.Utilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ProgramTests
    {
        private static readonly string[] HelpFlagArguments = { "-H" };
        private static readonly string[] ExecuteHelpCommandArguments = { "-X", "help" };
        private static readonly string[] ExecuteCommandMissingArgument = { "-X" };
        private static readonly string[] ExecuteUnknownCommandArguments = { "-X", "nope" };
        private static readonly string[] HardwireFlagArguments = { "-W" };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (CommandManager.Find("help") == null)
            {
                CommandManager.Initialize();
            }
        }

        [Test]
        public void CheckArgsNoArgumentsReturnsFalse()
        {
            bool handled = Program.CheckArgs(Array.Empty<string>(), NewShellContext());
            Assert.That(handled, Is.False);
        }

        [Test]
        public void CheckArgsHelpFlagWritesUsageAndReturnsTrue()
        {
            using ConsoleRedirectionScope console = new();

            bool handled = Program.CheckArgs(HelpFlagArguments, NewShellContext());

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(console.Writer.ToString(), Does.Contain("usage: NovaSharp"));
            });
        }

        [Test]
        public void CheckArgsExecuteCommandFlagRunsRequestedCommand()
        {
            using ConsoleRedirectionScope console = new();

            bool handled = Program.CheckArgs(ExecuteHelpCommandArguments, NewShellContext());

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(console.Writer.ToString(), Does.Contain("Commands:"));
            });
        }

        [Test]
        public void CheckArgsExecuteCommandFlagWithMissingArgumentShowsSyntax()
        {
            using ConsoleRedirectionScope console = new();

            bool handled = Program.CheckArgs(ExecuteCommandMissingArgument, NewShellContext());

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(console.Writer.ToString(), Does.Contain("Wrong syntax."));
            });
        }

        [Test]
        public void CheckArgsExecuteCommandFlagWithUnknownCommandReportsError()
        {
            using ConsoleRedirectionScope console = new();

            bool handled = Program.CheckArgs(ExecuteUnknownCommandArguments, NewShellContext());

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(console.Writer.ToString(), Does.Contain("Invalid command 'nope'."));
            });
        }

        [Test]
        public void CheckArgsRunScriptExecutesFile()
        {
            string scriptPath = Path.Combine(Path.GetTempPath(), $"sample_{Guid.NewGuid():N}.lua");
            File.WriteAllText(scriptPath, "return 42");

            using ConsoleRedirectionScope console = new();

            try
            {
                bool handled = Program.CheckArgs(new[] { scriptPath }, NewShellContext());
                Assert.That(handled, Is.True);
            }
            finally
            {
                File.Delete(scriptPath);
            }
        }

        [Test]
        public void CheckArgsRunScriptAppliesManifestCompatibility()
        {
            string modDirectory = Path.Combine(Path.GetTempPath(), $"mod_{Guid.NewGuid():N}");
            Directory.CreateDirectory(modDirectory);
            string scriptPath = Path.Combine(modDirectory, "entry.lua");
            File.WriteAllText(scriptPath, "if warn ~= nil then error('warn available') end");

            string manifestPath = Path.Combine(modDirectory, "mod.json");
            File.WriteAllText(
                manifestPath,
                "{\n"
                    + "    \"name\": \"CompatMod\",\n"
                    + "    \"luaCompatibility\": \"Lua53\"\n"
                    + "}"
            );

            using ConsoleRedirectionScope console = new();

            try
            {
                bool handled = Program.CheckArgs(new[] { scriptPath }, NewShellContext());

                Assert.Multiple(() =>
                {
                    Assert.That(handled, Is.True);
                    Assert.That(
                        console.Writer.ToString(),
                        Does.Contain("[compatibility] Applied Lua 5.3 profile")
                    );
                });
            }
            finally
            {
                if (Directory.Exists(modDirectory))
                {
                    Directory.Delete(modDirectory, recursive: true);
                }
            }
        }

        [Test]
        public void CheckArgsRunScriptLogsCompatibilitySummary()
        {
            string modDirectory = Path.Combine(Path.GetTempPath(), $"mod_{Guid.NewGuid():N}");
            Directory.CreateDirectory(modDirectory);
            string scriptPath = Path.Combine(modDirectory, "entry.lua");
            File.WriteAllText(scriptPath, "return 0");

            string manifestPath = Path.Combine(modDirectory, "mod.json");
            File.WriteAllText(
                manifestPath,
                "{\n"
                    + "    \"name\": \"CompatMod\",\n"
                    + "    \"luaCompatibility\": \"Lua52\"\n"
                    + "}"
            );

            using ConsoleRedirectionScope console = new();

            try
            {
                bool handled = Program.CheckArgs(new[] { scriptPath }, NewShellContext());

                Assert.Multiple(() =>
                {
                    Assert.That(handled, Is.True);
                    Assert.That(
                        console.Writer.ToString(),
                        Does.Contain("[compatibility] Running").And.Contain("Lua 5.2")
                    );
                });
            }
            finally
            {
                if (Directory.Exists(modDirectory))
                {
                    Directory.Delete(modDirectory, recursive: true);
                }
            }
        }

        [Test]
        public void CheckArgsHardwireFlagWithMissingArgumentsShowsSyntax()
        {
            using ConsoleRedirectionScope console = new();

            bool handled = Program.CheckArgs(HardwireFlagArguments, NewShellContext());

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(console.Writer.ToString(), Does.Contain("Wrong syntax."));
            });
        }

        [Test]
        public void CheckArgsHardwireFlagGeneratesDescriptors()
        {
            string dumpPath = Path.Combine(Path.GetTempPath(), $"dump_{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"hardwire_{Guid.NewGuid():N}.vb");

            Func<string, Table> originalLoader = HardwireCommand.DumpLoader;
            HardwireCommand.DumpLoader = _ =>
            {
                Script script = new(default(CoreModules));
                return CreateDescriptorTable(script, "internal");
            };

            using ConsoleRedirectionScope console = new();

            try
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
                    NewShellContext()
                );

                Assert.Multiple(() =>
                {
                    Assert.That(handled, Is.True);
                    Assert.That(
                        console.Writer.ToString(),
                        Does.Contain("done: 0 errors, 0 warnings.")
                    );
                    Assert.That(File.Exists(destPath), Is.True);
                    string generated = File.ReadAllText(destPath);
                    Assert.That(generated, Does.Contain("Namespace GeneratedNamespace"));
                    Assert.That(generated, Does.Contain("Class GeneratedTypes"));
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

        [Test]
        public void InterpreterLoopExecutesCommandsWhenNoPendingInput()
        {
            StubReplInterpreter interpreter = new();
            using ConsoleRedirectionScope console = new("!help" + Environment.NewLine);

            InvokeInterpreterLoop(interpreter, NewShellContext());

            string output = console.Writer.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(interpreter.EvaluateCalled, Is.False);
                Assert.That(output, Does.Contain("Commands:"));
            });
        }

        [Test]
        public void InterpreterLoopEvaluatesInputWhenCommandIsPending()
        {
            StubReplInterpreter interpreter = new()
            {
                PendingCommand = true,
                ReturnValue = DynValue.NewString("queued"),
            };
            using ConsoleRedirectionScope console = new("!help" + Environment.NewLine);

            InvokeInterpreterLoop(interpreter, NewShellContext());

            string output = console.Writer.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(interpreter.EvaluateCalled, Is.True);
                Assert.That(interpreter.LastInput, Is.EqualTo("!help"));
                Assert.That(output, Does.Contain("queued"));
            });
        }

        [Test]
        public void InterpreterLoopDoesNotPrintVoidResults()
        {
            StubReplInterpreter interpreter = new() { ReturnValue = DynValue.Void };
            using ConsoleRedirectionScope console = new("return 1" + Environment.NewLine);

            InvokeInterpreterLoop(interpreter, NewShellContext());

            string output = console.Writer.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(interpreter.EvaluateCalled, Is.True);
                Assert.That(output, Does.Not.Contain("Void"));
            });
        }

        [Test]
        public void InterpreterLoopPrintsInterpreterExceptionDecoratedMessage()
        {
            StubReplInterpreter interpreter = new()
            {
                ExceptionToThrow = new ScriptRuntimeException("boom")
                {
                    DecoratedMessage = "decorated message",
                },
            };
            using ConsoleRedirectionScope console = new("return 1" + Environment.NewLine);

            InvokeInterpreterLoop(interpreter, NewShellContext());

            string output = console.Writer.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(interpreter.EvaluateCalled, Is.True);
                Assert.That(output, Does.Contain("decorated message"));
            });
        }

        [Test]
        public void InterpreterLoopPrintsGeneralExceptionMessage()
        {
            StubReplInterpreter interpreter = new()
            {
                ExceptionToThrow = new InvalidOperationException("broken"),
            };
            using ConsoleRedirectionScope console = new("return 1" + Environment.NewLine);

            InvokeInterpreterLoop(interpreter, NewShellContext());

            string output = console.Writer.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(interpreter.EvaluateCalled, Is.True);
                Assert.That(output, Does.Contain("broken"));
            });
        }

        [Test]
        public void BannerPrintsCompatibilitySummary()
        {
            using ConsoleRedirectionScope console = new();

            Script script = new(CoreModules.PresetDefault);
            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua53;

            Program.ShowBannerForTests(script);

            string output = console.Writer.ToString();

            Assert.That(output, Does.Contain("[compatibility] Active profile: Lua 5.3"));
        }

        private static ShellContext NewShellContext()
        {
            return new ShellContext(new Interpreter.Script());
        }

        private static Table CreateDescriptorTable(Script script, string visibility)
        {
            Table descriptor = new(script);
            descriptor.Set(
                "class",
                DynValue.NewString(
                    "NovaSharp.Interpreter.Interop.StandardDescriptors.StandardUserDataDescriptor"
                )
            );
            descriptor.Set("visibility", DynValue.NewString(visibility));
            descriptor.Set("members", DynValue.NewTable(script));
            descriptor.Set("metamembers", DynValue.NewTable(script));

            Table root = new(script);
            root.Set("Sample", DynValue.NewTable(descriptor));
            return root;
        }

        private static void InvokeInterpreterLoop(
            ReplInterpreter interpreter,
            ShellContext shellContext
        )
        {
            Program.RunInterpreterLoopForTests(interpreter, shellContext);
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
