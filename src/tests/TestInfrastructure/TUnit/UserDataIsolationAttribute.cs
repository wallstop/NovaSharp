namespace NovaSharp.Interpreter.Tests
{
    using System;

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true
    )]
    public sealed class UserDataIsolationAttribute : Attribute
    {
        public UserDataIsolationAttribute(bool serialize = false)
        {
            Serialize = serialize;
        }

        public bool Serialize { get; }
    }
}
