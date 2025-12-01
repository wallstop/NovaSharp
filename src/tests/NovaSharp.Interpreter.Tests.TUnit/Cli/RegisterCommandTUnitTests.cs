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
    using NovaSharp.Tests.TestInfrastructure.Scopes;

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
            await ConsoleCaptureCoordinator
                .RunAsync(async () =>
                {
                    using ConsoleCaptureScope consoleScope = new(captureError: false);
                    RegisterCommand command = new();
                    ShellContext context = new(new Script());

                    command.Execute(context, "Missing.Namespace.TypeName");

                    string expected = CliMessages.RegisterCommandTypeNotFound(
                        "Missing.Namespace.TypeName"
                    );
                    await Assert
                        .That(consoleScope.Writer.ToString())
                        .Contains(expected)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithClrTypeRegistersItForUserDataInterop()
        {
            RegisterCommand command = new();
            ShellContext context = new(new Script());
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<SampleType>(ensureUnregistered: true);

            command.Execute(context, typeof(SampleType).AssemblyQualifiedName);

            bool registered = UserData.GetRegisteredTypes().Contains(typeof(SampleType));
            await Assert.That(registered).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithoutArgumentsListsRegisteredTypes()
        {
            await ConsoleCaptureCoordinator
                .RunAsync(async () =>
                {
                    using ConsoleCaptureScope consoleScope = new(captureError: false);
                    RegisterCommand command = new();
                    ShellContext context = new(new Script());
                    using UserDataRegistrationScope registrationScope =
                        UserDataRegistrationScope.Track<SampleType>();
                    UserData.RegisterType<SampleType>();

                    command.Execute(context, string.Empty);

                    await Assert
                        .That(consoleScope.Writer.ToString())
                        .Contains(typeof(SampleType).FullName)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        private sealed class SampleType { }
    }
}
