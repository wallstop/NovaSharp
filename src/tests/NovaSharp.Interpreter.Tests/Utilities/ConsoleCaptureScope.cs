namespace NovaSharp.Interpreter.Tests.Utilities
{
    using System;
    using System.IO;

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
            Writer.Dispose();
        }
    }
}
