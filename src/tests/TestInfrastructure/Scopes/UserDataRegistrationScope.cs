namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop;

    /// <summary>
    /// Tracks user-data registrations and guarantees they are unregistered when disposed.
    /// </summary>
    internal sealed class UserDataRegistrationScope : IDisposable
    {
        private readonly HashSet<Type> _trackedTypes = new();
        private readonly IDisposable _isolationScope;
        private bool _disposed;

        private UserDataRegistrationScope()
        {
            _isolationScope = UserData.BeginIsolationScope();
        }

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

        public static UserDataRegistrationScope Track(bool ensureUnregistered, params Type[] types)
        {
            UserDataRegistrationScope scope = new();
            scope.AddRange(types, ensureUnregistered);
            return scope;
        }

        public static UserDataRegistrationScope Track(
            IEnumerable<Type> types,
            bool ensureUnregistered = false
        )
        {
            UserDataRegistrationScope scope = new();
            scope.AddRange(types, ensureUnregistered);
            return scope;
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

        public void RegisterExtensionType<T>(InteropAccessMode mode = InteropAccessMode.Default)
        {
            RegisterExtensionType(typeof(T), mode);
        }

        public void RegisterExtensionType(
            Type type,
            InteropAccessMode mode = InteropAccessMode.Default
        )
        {
            ArgumentNullException.ThrowIfNull(type);
            ObjectDisposedException.ThrowIf(_disposed, nameof(UserDataRegistrationScope));
            UserData.RegisterExtensionType(type, mode);
        }

        public IUserDataDescriptor RegisterType<T>(
            InteropAccessMode accessMode = InteropAccessMode.Default,
            string friendlyName = null,
            bool ensureUnregistered = false
        )
        {
            Add<T>(ensureUnregistered);
            return UserData.RegisterType<T>(accessMode, friendlyName);
        }

        public IUserDataDescriptor RegisterType(
            Type type,
            InteropAccessMode accessMode = InteropAccessMode.Default,
            string friendlyName = null,
            bool ensureUnregistered = false
        )
        {
            Add(type, ensureUnregistered);
            return UserData.RegisterType(type, accessMode, friendlyName);
        }

        public IUserDataDescriptor RegisterType<T>(
            IUserDataDescriptor customDescriptor,
            bool ensureUnregistered = false
        )
        {
            Add<T>(ensureUnregistered);
            return UserData.RegisterType<T>(customDescriptor);
        }

        public IUserDataDescriptor RegisterType(
            Type type,
            IUserDataDescriptor customDescriptor,
            bool ensureUnregistered = false
        )
        {
            Add(type, ensureUnregistered);
            return UserData.RegisterType(type, customDescriptor);
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

            _isolationScope.Dispose();
            _disposed = true;
        }

        private void AddRange(IEnumerable<Type> types, bool ensureUnregistered)
        {
            ArgumentNullException.ThrowIfNull(types);

            foreach (Type type in types)
            {
                Add(type, ensureUnregistered);
            }
        }
    }
}
