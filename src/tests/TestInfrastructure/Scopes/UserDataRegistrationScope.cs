namespace NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Tracks user-data registrations and guarantees they are unregistered when disposed.
    /// </summary>
    internal sealed class UserDataRegistrationScope : IDisposable
    {
        private readonly HashSet<Type> _trackedTypes = new();
        private bool _disposed;

        private UserDataRegistrationScope() { }

        public static UserDataRegistrationScope Track(Type type, bool ensureUnregistered = false)
        {
            UserDataRegistrationScope scope = new();
            scope.Add(type, ensureUnregistered);
            return scope;
        }

        public static UserDataRegistrationScope Track<T>(bool ensureUnregistered = false)
        {
            return Track(typeof(T), ensureUnregistered);
        }

        public static UserDataRegistrationScope Create()
        {
            return new UserDataRegistrationScope();
        }

        public void Add(Type type, bool ensureUnregistered = false)
        {
            ArgumentNullException.ThrowIfNull(type);

            ObjectDisposedException.ThrowIf(_disposed, nameof(UserDataRegistrationScope));

            if (!_trackedTypes.Add(type))
            {
                return;
            }

            if (ensureUnregistered && UserData.IsTypeRegistered(type))
            {
                UserData.UnregisterType(type);
            }
        }

        public void Add<T>(bool ensureUnregistered = false)
        {
            Add(typeof(T), ensureUnregistered);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (Type type in _trackedTypes)
            {
                if (UserData.IsTypeRegistered(type))
                {
                    UserData.UnregisterType(type);
                }
            }

            _disposed = true;
        }
    }
}
