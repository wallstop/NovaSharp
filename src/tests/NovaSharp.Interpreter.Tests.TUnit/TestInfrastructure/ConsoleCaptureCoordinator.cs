namespace NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    internal static class ConsoleCaptureCoordinator
    {
        internal static readonly SemaphoreSlim Semaphore = new(1, 1);

        internal static async Task RunAsync(Func<Task> callback)
        {
            SemaphoreSlimLease lease = await SemaphoreSlimScope
                .WaitAsync(Semaphore)
                .ConfigureAwait(false);

            try
            {
                await callback().ConfigureAwait(false);
            }
            finally
            {
                if (lease != null)
                {
                    await lease.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
