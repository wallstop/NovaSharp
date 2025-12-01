namespace NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Provides a disposable wrapper for overriding <see cref="HardwireCommand.DumpLoader" />.
    /// </summary>
    internal sealed class HardwireDumpLoaderScope : IDisposable
    {
        private readonly Func<string, Table> _previousLoader;
        private bool _disposed;

        private HardwireDumpLoaderScope(Func<string, Table> newLoader)
        {
            ArgumentNullException.ThrowIfNull(newLoader);
            _previousLoader = HardwireCommand.DumpLoader;
            HardwireCommand.DumpLoader = newLoader;
        }

        public static HardwireDumpLoaderScope Override(Func<string, Table> newLoader)
        {
            return new HardwireDumpLoaderScope(newLoader);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            HardwireCommand.DumpLoader = _previousLoader;
            _disposed = true;
        }
    }
}
