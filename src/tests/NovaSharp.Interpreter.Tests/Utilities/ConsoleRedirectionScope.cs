namespace NovaSharp.Interpreter.Tests.Utilities
{
    using System;
    using System.IO;

    internal sealed class ConsoleRedirectionScope : IDisposable
    {
        private readonly TextWriter _originalOut;
        private readonly TextReader _originalIn;
        private readonly StringReader _inputReader;

        public ConsoleRedirectionScope(string input = null)
        {
            Writer = new StringWriter();
            _originalOut = Console.Out;
            _originalIn = Console.In;

            Console.SetOut(Writer);

            if (input != null)
            {
                _inputReader = new StringReader(input);
                Console.SetIn(_inputReader);
            }
            else
            {
                _inputReader = null;
            }
        }

        public StringWriter Writer { get; }

        public void Dispose()
        {
            Console.SetOut(_originalOut);
            Console.SetIn(_originalIn);
            Writer.Dispose();
            _inputReader?.Dispose();
        }
    }
}
