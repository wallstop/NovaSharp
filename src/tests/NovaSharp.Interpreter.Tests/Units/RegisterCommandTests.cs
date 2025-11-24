namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using System.Linq;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RegisterCommandTests
    {
        private TextWriter _originalOut = null!;
        private StringWriter _writer = null!;
        private ShellContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            _context = new ShellContext(new Script());
            _writer = new StringWriter();
            _originalOut = Console.Out;
            Console.SetOut(_writer);
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(_originalOut);
            _writer.Dispose();
            UserData.UnregisterType<SampleType>();
        }

        [Test]
        public void ExecuteWithUnknownTypeWritesError()
        {
            RegisterCommand command = new();

            command.Execute(_context, "Missing.Namespace.TypeName");

            Assert.That(
                _writer.ToString(),
                Does.Contain("Type Missing.Namespace.TypeName not found.")
            );
        }

        [Test]
        public void ExecuteWithClrTypeRegistersItForUserDataInterop()
        {
            RegisterCommand command = new();
            UserData.UnregisterType<SampleType>();

            command.Execute(_context, typeof(SampleType).AssemblyQualifiedName);

            bool registered = UserData.GetRegisteredTypes().Contains(typeof(SampleType));

            Assert.That(registered, Is.True);
        }

        [Test]
        public void ExecuteWithoutArgumentsListsRegisteredTypes()
        {
            RegisterCommand command = new();
            UserData.RegisterType<SampleType>();

            command.Execute(_context, string.Empty);

            Assert.That(_writer.ToString(), Does.Contain(typeof(SampleType).FullName));
        }

        public sealed class SampleType { }
    }
}
