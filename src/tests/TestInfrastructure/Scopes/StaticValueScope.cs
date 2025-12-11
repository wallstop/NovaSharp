namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;

    /// <summary>
    /// Captures a static value exposed via getter/setter delegates and restores it on disposal.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    internal sealed class StaticValueScope<T> : IDisposable
    {
        private readonly Action<T> _setter;
        private readonly T _originalValue;
        private bool _disposed;

        private StaticValueScope(Func<T> getter, Action<T> setter, T newValue, bool applyChange)
        {
            ArgumentNullException.ThrowIfNull(getter);
            ArgumentNullException.ThrowIfNull(setter);

            _setter = setter;
            _originalValue = getter();

            if (applyChange)
            {
                setter(newValue);
            }
        }

        public static StaticValueScope<T> Override(Func<T> getter, Action<T> setter, T newValue)
        {
            return new StaticValueScope<T>(getter, setter, newValue, applyChange: true);
        }

        public static StaticValueScope<T> Capture(Func<T> getter, Action<T> setter)
        {
            return new StaticValueScope<T>(getter, setter, default, applyChange: false);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _setter(_originalValue);
            _disposed = true;
        }
    }
}
