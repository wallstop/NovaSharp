namespace NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class ConsoleCaptureCoordinator
    {
        internal static readonly SemaphoreSlim Semaphore = new(1, 1);

        internal static async Task RunAsync(Func<Task> callback)
        {
            await Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                await callback().ConfigureAwait(false);
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }
}
