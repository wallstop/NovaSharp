namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Linq;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Tests.Utilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RegisterCommandTests : IDisposable
    {
        private ConsoleCaptureScope _consoleScope;
        private ShellContext _context;

        static RegisterCommandTests()
        {
            _ = new SampleType();
        }

        [SetUp]
        public void SetUp()
        {
            _context = new ShellContext(new Script());
            _consoleScope = new ConsoleCaptureScope(captureError: false);
        }

        [TearDown]
        public void TearDown()
        {
            Dispose();
            UserData.UnregisterType<SampleType>();
        }

        [Test]
        public void ExecuteWithUnknownTypeWritesError()
        {
            RegisterCommand command = new();

            command.Execute(_context, "Missing.Namespace.TypeName");

            Assert.That(
                _consoleScope!.Writer.ToString(),
                Does.Contain(CliMessages.RegisterCommandTypeNotFound("Missing.Namespace.TypeName"))
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

            Assert.That(
                _consoleScope!.Writer.ToString(),
                Does.Contain(typeof(SampleType).FullName)
            );
        }

        private sealed class SampleType { }

        public void Dispose()
        {
            if (_consoleScope != null)
            {
                _consoleScope.Dispose();
                _consoleScope = null;
            }
        }
    }
}
