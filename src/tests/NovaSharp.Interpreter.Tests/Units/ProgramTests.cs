namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using Commands;
    using Commands.Implementations;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ProgramTests
    {
        private TextWriter _originalOut = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (CommandManager.Find("help") == null)
            {
                CommandManager.Initialize();
            }
        }

        [SetUp]
        public void SetUp()
        {
            _originalOut = Console.Out;
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(_originalOut);
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
            using StringWriter writer = new();
            Console.SetOut(writer);

            bool handled = Program.CheckArgs(new[] { "-H" }, NewShellContext());

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(writer.ToString(), Does.Contain("usage: NovaSharp"));
            });
        }

        [Test]
        public void CheckArgsExecuteCommandFlagRunsRequestedCommand()
        {
            using StringWriter writer = new();
            Console.SetOut(writer);

            bool handled = Program.CheckArgs(new[] { "-X", "help" }, NewShellContext());

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(writer.ToString(), Does.Contain("Commands:"));
            });
        }

        [Test]
        public void CheckArgsExecuteCommandFlagWithMissingArgumentShowsSyntax()
        {
            using StringWriter writer = new();
            Console.SetOut(writer);

            bool handled = Program.CheckArgs(new[] { "-X" }, NewShellContext());

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(writer.ToString(), Does.Contain("Wrong syntax."));
            });
        }

        [Test]
        public void CheckArgsExecuteCommandFlagWithUnknownCommandReportsError()
        {
            using StringWriter writer = new();
            Console.SetOut(writer);

            bool handled = Program.CheckArgs(new[] { "-X", "nope" }, NewShellContext());

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(writer.ToString(), Does.Contain("Invalid command 'nope'."));
            });
        }

        [Test]
        public void CheckArgsRunScriptExecutesFile()
        {
            string scriptPath = Path.Combine(Path.GetTempPath(), $"sample_{Guid.NewGuid():N}.lua");
            File.WriteAllText(scriptPath, "return 42");

            using StringWriter writer = new();
            Console.SetOut(writer);

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
        public void CheckArgsHardwireFlagWithMissingArgumentsShowsSyntax()
        {
            using StringWriter writer = new();
            Console.SetOut(writer);

            bool handled = Program.CheckArgs(new[] { "-W" }, NewShellContext());

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(writer.ToString(), Does.Contain("Wrong syntax."));
            });
        }

        [Test]
        public void CheckArgsHardwireFlagGeneratesDescriptors()
        {
            string dumpPath = Path.Combine(Path.GetTempPath(), $"dump_{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"hardwire_{Guid.NewGuid():N}.vb");

            Func<string, Table> originalLoader = HardWireCommand.DumpLoader;
            HardWireCommand.DumpLoader = _ =>
            {
                Script script = new(CoreModules.None);
                return CreateDescriptorTable(script, "internal");
            };

            using StringWriter writer = new();
            Console.SetOut(writer);

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
                    Assert.That(writer.ToString(), Does.Contain("done: 0 errors, 0 warnings."));
                    Assert.That(File.Exists(destPath), Is.True);
                    string generated = File.ReadAllText(destPath);
                    Assert.That(generated, Does.Contain("Namespace GeneratedNamespace"));
                    Assert.That(generated, Does.Contain("Class GeneratedTypes"));
                });
            }
            finally
            {
                HardWireCommand.DumpLoader = originalLoader;

                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }
            }
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
    }
}
