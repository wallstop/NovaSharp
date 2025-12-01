#pragma warning disable CA2007

namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;

    [PlatformDetectorIsolation]
    [UserDataIsolation]
    public sealed class RegisterCommandTUnitTests
    {
        static RegisterCommandTUnitTests()
        {
            _ = new SampleType();
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithUnknownTypeWritesError()
        {
            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleCaptureScope consoleScope = new(captureError: false);
                RegisterCommand command = new();
                ShellContext context = new(new Script());

                command.Execute(context, "Missing.Namespace.TypeName");

                string expected = CliMessages.RegisterCommandTypeNotFound(
                    "Missing.Namespace.TypeName"
                );
                await Assert.That(consoleScope.Writer.ToString()).Contains(expected);
            });
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithClrTypeRegistersItForUserDataInterop()
        {
            RegisterCommand command = new();
            ShellContext context = new(new Script());
            UserData.UnregisterType<SampleType>();

            command.Execute(context, typeof(SampleType).AssemblyQualifiedName);

            bool registered = UserData.GetRegisteredTypes().Contains(typeof(SampleType));
            await Assert.That(registered).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithoutArgumentsListsRegisteredTypes()
        {
            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleCaptureScope consoleScope = new(captureError: false);
                RegisterCommand command = new();
                ShellContext context = new(new Script());
                UserData.RegisterType<SampleType>();

                command.Execute(context, string.Empty);

                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(typeof(SampleType).FullName);
            });
        }

        private sealed class SampleType { }
    }
}

#pragma warning restore CA2007
