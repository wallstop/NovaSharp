namespace NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.RegistrationPolicies;

    /// <summary>
    /// Temporarily overrides <see cref="UserData.RegistrationPolicy"/> and restores it on disposal.
    /// </summary>
    internal sealed class UserDataRegistrationPolicyScope : IDisposable
    {
        private readonly IRegistrationPolicy _previousPolicy;
        private bool _disposed;

        private UserDataRegistrationPolicyScope(IRegistrationPolicy nextPolicy)
        {
            ArgumentNullException.ThrowIfNull(nextPolicy);
            _previousPolicy = UserData.RegistrationPolicy;
            UserData.RegistrationPolicy = nextPolicy;
        }

        public static UserDataRegistrationPolicyScope Override(IRegistrationPolicy nextPolicy)
        {
            return new UserDataRegistrationPolicyScope(nextPolicy);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            UserData.RegistrationPolicy = _previousPolicy;
            _disposed = true;
        }
    }
}
