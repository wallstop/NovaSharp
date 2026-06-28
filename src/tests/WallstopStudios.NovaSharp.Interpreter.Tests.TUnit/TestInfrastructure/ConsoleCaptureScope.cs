namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using System;
    using System.IO;

#pragma warning disable TUnit0055
    internal sealed class ConsoleCaptureScope : IDisposable
    {
        private readonly TextWriter _originalOut;
        private readonly TextWriter _originalError;

        public ConsoleCaptureScope(bool captureError)
        {
            // Use ThreadSafeStringWriter instead of StringWriter to prevent race conditions.
            // StringWriter uses StringBuilder internally, which is not thread-safe.
            // When both Console.Out and Console.Error are redirected to the same writer,
            // concurrent writes can corrupt StringBuilder's internal state, causing
            // ArgumentOutOfRangeException ("chunkLength") on ToString().
            Writer = new ThreadSafeStringWriter();
            _originalOut = Console.Out;
            _originalError = Console.Error;

            Console.SetOut(Writer);

            if (captureError)
            {
                Console.SetError(Writer);
            }
        }

        public ThreadSafeStringWriter Writer { get; }

        public void Dispose()
        {
            Console.SetOut(_originalOut);
            Console.SetError(_originalError);
        }
    }
#pragma warning restore TUnit0055
}
