namespace NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;

    /// <summary>
    /// Temporarily overrides an environment variable and restores the previous value when disposed.
    /// </summary>
    internal sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _name;
        private readonly string _originalValue;
        private readonly bool _hadOriginalValue;
        private readonly EnvironmentVariableTarget _target;
        private bool _disposed;

        private EnvironmentVariableScope(
            string name,
            string value,
            EnvironmentVariableTarget target,
            bool clearValue
        )
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Variable name must be provided.", nameof(name));
            }

            _name = name;
            _target = target;
            _originalValue = Environment.GetEnvironmentVariable(name, target);
            _hadOriginalValue = _originalValue != null;

            Environment.SetEnvironmentVariable(name, clearValue ? null : value, target);
        }

        public static EnvironmentVariableScope Override(
            string name,
            string value,
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process
        )
        {
            return new EnvironmentVariableScope(name, value, target, clearValue: false);
        }

        public static EnvironmentVariableScope Clear(
            string name,
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process
        )
        {
            return new EnvironmentVariableScope(name, value: null, target, clearValue: true);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Environment.SetEnvironmentVariable(
                _name,
                _hadOriginalValue ? _originalValue : null,
                _target
            );

            _disposed = true;
        }
    }
}
