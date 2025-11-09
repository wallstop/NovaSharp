namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp;
    using NovaSharp.Commands.Implementations;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HardWireCommandTests
    {
        private TextWriter _originalOut = null!;
        private TextReader _originalIn = null!;

        [SetUp]
        public void SetUp()
        {
            _originalOut = Console.Out;
            _originalIn = Console.In;
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(_originalOut);
            Console.SetIn(_originalIn);
        }

        [Test]
        public void Execute_AbortOnQuit_StopsInteractiveFlow()
        {
            HardWireCommand command = new();
            using StringWriter writer = new();
            Console.SetOut(writer);
            Console.SetIn(new StringReader("#quit\n"));

            command.Execute(new ShellContext(new Interpreter.Script()), string.Empty);

            Assert.That(writer.ToString(), Does.Contain("At any question, type #quit to abort."));
        }

        [Test]
        public void Execute_InvalidLuaFile_PromptsForRetry()
        {
            HardWireCommand command = new();
            using StringWriter writer = new();
            Console.SetOut(writer);
            Console.SetIn(new StringReader("cs\nnonexistent.lua\n#quit\n"));

            command.Execute(new ShellContext(new Interpreter.Script()), string.Empty);

            Assert.That(writer.ToString(), Does.Contain("File does not exists."));
        }

        [Test]
        public void Generate_WithMissingDumpFile_ReportsInternalError()
        {
            using StringWriter writer = new();
            Console.SetOut(writer);

            string dumpPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");

            try
            {
                HardWireCommand.Generate(
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

            Assert.That(writer.ToString(), Does.Contain("Internal error"));
        }

        [Test]
        public void Execute_InvalidNamespaceRequestsRetry()
        {
            string dumpPath = Path.Combine(Path.GetTempPath(), $"dump_{Guid.NewGuid():N}.lua");
            string destPath = Path.Combine(Path.GetTempPath(), $"hardwire_{Guid.NewGuid():N}.cs");
            File.WriteAllText(dumpPath, "return {}");

            HardWireCommand command = new();
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

            Assert.That(writer.ToString(), Does.Contain("Not a valid identifier."));

            File.Delete(dumpPath);
            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }
        }
    }
}
