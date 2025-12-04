namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using NovaSharp.Tests.TestInfrastructure.Scopes;
    using static NovaSharp.Interpreter.Tests.TUnit.Cli.CliTestHelpers;

    public sealed class RegisterCommandTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ExecuteWithUnknownTypePrintsError()
        {
            RegisterCommand command = new();
            ShellContext context = CreateShellContext();

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
            ShellContext context = CreateShellContext();
            Type targetType = typeof(SampleUserData);

            using UserDataRegistrationScope registrationScope = UserDataRegistrationScope.Track(
                targetType,
                ensureUnregistered: true
            );

            await WithConsoleAsync(console =>
                {
                    command.Execute(context, targetType.AssemblyQualifiedName);
                    _ = console;
                    return Task.CompletedTask;
                })
                .ConfigureAwait(false);

            _ = new SampleUserData();

            HashSet<Type> historicalTypes = new(
                UserData.GetRegisteredTypes(useHistoricalData: true)
            );

            await Assert.That(historicalTypes.Contains(targetType)).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithoutArgumentsListsRegisteredTypes()
        {
            RegisterCommand command = new();
            ShellContext context = CreateShellContext();

            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<SampleUserData>(ensureUnregistered: true);

            await WithConsoleAsync(async console =>
                {
                    registrationScope.RegisterType<SampleUserData>();
                    await Assert
                        .That(UserData.IsTypeRegistered<SampleUserData>())
                        .IsTrue()
                        .ConfigureAwait(false);
                    command.Execute(context, string.Empty);

                    await Assert
                        .That(console.Writer.ToString())
                        .Contains(typeof(SampleUserData).FullName)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        private static Task WithConsoleAsync(Func<ConsoleRedirectionScope, Task> action)
        {
            return ConsoleTestUtilities.WithConsoleRedirectionAsync(action);
        }

        private sealed class SampleUserData
        {
            public string Name { get; set; } = "Nova";
        }
    }
}
