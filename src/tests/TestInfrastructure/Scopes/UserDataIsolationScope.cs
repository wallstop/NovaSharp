namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Provides a disposable scope that isolates global UserData registrations per test.
    /// </summary>
    internal sealed class UserDataIsolationScope : IDisposable
    {
        private readonly IDisposable _scope;
        private bool _disposed;

        private UserDataIsolationScope(IDisposable scope)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public static UserDataIsolationScope Begin()
        {
            IDisposable scope = UserData.BeginIsolationScope();
            return new UserDataIsolationScope(scope);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _scope.Dispose();
            _disposed = true;
        }
    }
}
