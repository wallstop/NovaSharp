namespace NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using System.Threading;

    internal static class ConsoleCaptureCoordinator
    {
        internal static readonly SemaphoreSlim Semaphore = new(1, 1);
    }
}
