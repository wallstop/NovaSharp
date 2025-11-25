namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HardwireCommandTests
    {
        private TextWriter _originalOut = null!;
        private TextReader _originalIn = null!;
        private Func<string, Table> _originalDumpLoader = null!;

        [SetUp]
        public void SetUp()
        {
            _originalOut = Console.Out;
            _originalIn = Console.In;
            _originalDumpLoader = HardwireCommand.DumpLoader;
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(_originalOut);
            Console.SetIn(_originalIn);
            HardwireCommand.DumpLoader = _originalDumpLoader;
        }

        [Test]
        public void ExecuteAbortOnQuitStopsInteractiveFlow()
        {
            HardwireCommand command = new();
            using StringWriter writer = new();
            Console.SetOut(writer);
            Console.SetIn(new StringReader("#quit\n"));

            command.Execute(new ShellContext(new Interpreter.Script()), string.Empty);

            Assert.That(writer.ToString(), Does.Contain(CliMessages.HardwireCommandAbortHint));
        }

        [Test]
        public void ExecuteInvalidLuaFilePromptsForRetry()
        {
            HardwireCommand command = new();
            using StringWriter writer = new();
            Console.SetOut(writer);
            Console.SetIn(new StringReader("cs\nnonexistent.lua\n#quit\n"));

            command.Execute(new ShellContext(new Interpreter.Script()), string.Empty);

            Assert.That(writer.ToString(), Does.Contain(CliMessages.HardwireMissingFile));
        }

        [Test]
        public void GenerateWithMissingDumpFileReportsInternalError()
        {
            using StringWriter writer = new();
            Console.SetOut(writer);

            string dumpPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");

            try
            {
                HardwireCommand.Generate(
                    "cs",
                    dumpPath,
                    destPath,
                    false,
                    "MissingTypes",
                    "MissingNs"
                );
            }
            finally
            {
                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }
            }

            Assert.That(
                writer.ToString(),
                Does.Contain(CliMessages.HardwireInternalError(string.Empty))
            );
        }

        [Test]
        public void ExecuteInvalidNamespaceRequestsRetry()
        {
            string dumpPath = Path.Combine(Path.GetTempPath(), $"dump_{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"hardwire_{Guid.NewGuid():N}.cs");
            File.WriteAllText(dumpPath, "return {}");

            HardwireCommand command = new();
            using StringWriter writer = new();
            Console.SetOut(writer);
            Console.SetIn(
                new StringReader(
                    string.Join(
                        Environment.NewLine,
                        new[] { "cs", dumpPath, destPath, "y", "123Invalid", "#quit" }
                    )
                )
            );

            command.Execute(new ShellContext(new Interpreter.Script()), string.Empty);

            Assert.That(writer.ToString(), Does.Contain(CliMessages.HardwireIdentifierValidation));

            File.Delete(dumpPath);
            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }
        }

        [Test]
        public void GenerateCreatesCSharpSourceFromDump()
        {
            string dumpPath = Path.Combine(Path.GetTempPath(), $"dump_{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"hardwire_{Guid.NewGuid():N}.cs");

            HardwireCommand.DumpLoader = _ =>
            {
                Script script = new(default(CoreModules));
                return CreateDescriptorTable(script, "public");
            };

            using StringWriter writer = new();
            Console.SetOut(writer);
            string output = string.Empty;

            try
            {
                HardwireCommand.Generate(
                    "cs",
                    dumpPath,
                    destPath,
                    false,
                    "GeneratedTypes",
                    "GeneratedNamespace"
                );
            }
            finally
            {
                Console.SetOut(_originalOut);
                output = writer.ToString();
            }

            try
            {
                Assert.That(
                    File.Exists(destPath),
                    Is.True,
                    $"Console output:{Environment.NewLine}{output}"
                );
                string generated = File.ReadAllText(destPath);

                Assert.Multiple(() =>
                {
                    Assert.That(generated, Does.Contain("namespace GeneratedNamespace"));
                    Assert.That(generated, Does.Contain("class GeneratedTypes"));
                    Assert.That(output, Does.Contain(CliMessages.HardwireGenerationSummary(0, 0)));
                    Assert.That(output, Does.Not.Contain("Internal error"));
                });
            }
            finally
            {
                File.Delete(dumpPath);
                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }
            }
        }

        [Test]
        public void GenerateWithInternalVisibilityAndNoInternalsEmitsWarning()
        {
            string dumpPath = Path.Combine(Path.GetTempPath(), $"dump_{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"hardwire_{Guid.NewGuid():N}.cs");

            HardwireCommand.DumpLoader = _ =>
            {
                Script script = new(default(CoreModules));
                return CreateDescriptorTable(script, "internal");
            };

            using StringWriter writer = new();
            Console.SetOut(writer);
            string output = string.Empty;

            try
            {
                HardwireCommand.Generate(
                    "cs",
                    dumpPath,
                    destPath,
                    false,
                    "GeneratedTypes",
                    "GeneratedNamespace"
                );
            }
            finally
            {
                Console.SetOut(_originalOut);
                output = writer.ToString();
            }

            try
            {
                Assert.That(
                    File.Exists(destPath),
                    Is.True,
                    $"Console output:{Environment.NewLine}{output}"
                );

                Assert.Multiple(() =>
                {
                    Assert.That(output, Does.Contain("visibility is 'internal'"));
                    Assert.That(output, Does.Contain(CliMessages.HardwireGenerationSummary(0, 1)));
                });
            }
            finally
            {
                File.Delete(dumpPath);
                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }
            }
        }

        [Test]
        public void GenerateCreatesVbSourceWhenRequested()
        {
            string dumpPath = Path.Combine(Path.GetTempPath(), $"dump_{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"hardwire_{Guid.NewGuid():N}.vb");

            HardwireCommand.DumpLoader = _ =>
            {
                Script script = new(default(CoreModules));
                return CreateDescriptorTable(script, "public");
            };

            using StringWriter writer = new();
            Console.SetOut(writer);
            string output = string.Empty;

            try
            {
                HardwireCommand.Generate(
                    "vb",
                    dumpPath,
                    destPath,
                    true,
                    "GeneratedTypes",
                    "GeneratedNamespace"
                );
            }
            finally
            {
                Console.SetOut(_originalOut);
                output = writer.ToString();
            }

            try
            {
                Assert.That(
                    File.Exists(destPath),
                    Is.True,
                    $"Console output:{Environment.NewLine}{output}"
                );
                string generated = File.ReadAllText(destPath);

                Assert.Multiple(() =>
                {
                    Assert.That(generated, Does.Contain("Namespace GeneratedNamespace"));
                    Assert.That(generated, Does.Contain("Class GeneratedTypes"));
                    Assert.That(output, Does.Contain(CliMessages.HardwireGenerationSummary(0, 0)));
                });
            }
            finally
            {
                File.Delete(dumpPath);
                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }
            }
        }

        [Test]
        public void GenerateWithInternalsAllowedSuppressesWarning()
        {
            string dumpPath = Path.Combine(Path.GetTempPath(), $"dump_{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"hardwire_{Guid.NewGuid():N}.cs");

            HardwireCommand.DumpLoader = _ =>
            {
                Script script = new(default(CoreModules));
                return CreateDescriptorTable(script, "internal");
            };

            using StringWriter writer = new();
            Console.SetOut(writer);
            string output = string.Empty;

            try
            {
                HardwireCommand.Generate(
                    "cs",
                    dumpPath,
                    destPath,
                    true,
                    "GeneratedTypes",
                    "GeneratedNamespace"
                );
            }
            finally
            {
                Console.SetOut(_originalOut);
                output = writer.ToString();
            }

            try
            {
                Assert.That(
                    File.Exists(destPath),
                    Is.True,
                    $"Console output:{Environment.NewLine}{output}"
                );

                Assert.Multiple(() =>
                {
                    Assert.That(output, Does.Not.Contain("visibility is 'internal'"));
                    Assert.That(output, Does.Contain(CliMessages.HardwireGenerationSummary(0, 0)));
                });
            }
            finally
            {
                File.Delete(dumpPath);
                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }
            }
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
