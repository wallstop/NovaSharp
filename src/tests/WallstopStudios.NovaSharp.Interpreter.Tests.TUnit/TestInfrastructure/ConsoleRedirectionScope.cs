namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using System;
    using System.IO;

#pragma warning disable TUnit0055
    internal sealed class ConsoleRedirectionScope : IDisposable
    {
        private readonly TextWriter _originalOut;
        private readonly TextReader _originalIn;
        private readonly TextReader _inputReader;

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
            _inputReader?.Dispose();
        }
    }
#pragma warning restore TUnit0055
}
