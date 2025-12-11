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
            Writer = new StringWriter();
            _originalOut = Console.Out;
            _originalError = Console.Error;

            Console.SetOut(Writer);

            if (captureError)
            {
                Console.SetError(Writer);
            }
        }

        public StringWriter Writer { get; }

        public void Dispose()
        {
            Console.SetOut(_originalOut);
            Console.SetError(_originalError);
        }
    }
#pragma warning restore TUnit0055
}
