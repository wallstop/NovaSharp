namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using System;
    using System.Threading.Tasks;

    internal static class ConsoleTestUtilities
    {
        public static Task WithConsoleCaptureAsync(
            Func<ConsoleCaptureScope, Task> action,
            bool captureError = true
        )
        {
            ArgumentNullException.ThrowIfNull(action);

            return ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleCaptureScope captureScope = new(captureError);
                await action(captureScope).ConfigureAwait(false);
            });
        }

        public static Task WithConsoleRedirectionAsync(
            Func<ConsoleRedirectionScope, Task> action,
            string input = null
        )
        {
            ArgumentNullException.ThrowIfNull(action);

            return ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleRedirectionScope redirectionScope = new(input);
                await action(redirectionScope).ConfigureAwait(false);
            });
        }
    }
}
