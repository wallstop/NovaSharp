namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    internal static class ConsoleCaptureCoordinator
    {
        internal static readonly SemaphoreSlim Semaphore = new(1, 1);

        internal static async Task RunAsync(Func<Task> callback)
        {
            SemaphoreSlimLease lease = await SemaphoreSlimScope
                .WaitAsync(Semaphore)
                .ConfigureAwait(false);
            await using ConfiguredAsyncDisposable leaseScope = lease.ConfigureAwait(false);

            await callback().ConfigureAwait(false);
        }
    }
}
