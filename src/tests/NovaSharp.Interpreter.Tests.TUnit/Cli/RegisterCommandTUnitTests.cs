namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    public sealed class RegisterCommandTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ExecuteWithUnknownTypePrintsError()
        {
            RegisterCommand command = new();
            ShellContext context = new(new Script());

            await WithConsoleAsync(async console =>
                {
                    command.Execute(context, "Unknown.Namespace.Type");

                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(CliMessages.RegisterCommandTypeNotFound("Unknown.Namespace.Type"))
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithValidTypeRegistersUserData()
        {
            RegisterCommand command = new();
            ShellContext context = new(new Script());

            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<SampleUserData>(ensureUnregistered: true);

            await WithConsoleAsync(async console =>
                {
                    command.Execute(context, typeof(SampleUserData).AssemblyQualifiedName);
                    _ = console;
                })
                .ConfigureAwait(false);

            _ = new SampleUserData();
            bool isRegistered = UserData.GetRegisteredTypes().Contains(typeof(SampleUserData));
            await Assert.That(isRegistered).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithoutArgumentsListsRegisteredTypes()
        {
            RegisterCommand command = new();
            ShellContext context = new(new Script());

            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<SampleUserData>(ensureUnregistered: true);

            await WithConsoleAsync(async console =>
                {
                    UserData.RegisterType(typeof(SampleUserData));
                    command.Execute(context, string.Empty);

                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(typeof(SampleUserData).FullName)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        private static async Task WithConsoleAsync(Func<ConsoleRedirectionScope, Task> action)
        {
            await ConsoleCaptureCoordinator
                .RunAsync(async () =>
                {
                    using ConsoleRedirectionScope console = new();
                    await action(console).ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        private sealed class SampleUserData
        {
            public string Name { get; set; } = "Nova";
        }
    }
}
