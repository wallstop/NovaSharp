namespace NovaSharp.Interpreter.Errors
{
    using System;

    /// <summary>
    /// Exception thrown when an inconsistent state is reached in the interpreter
    /// </summary>
#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    [Serializable]
#endif
    public class InternalErrorException : InterpreterException
    {
        public InternalErrorException()
            : base("Internal error") { }

        public InternalErrorException(string message)
            : base(message) { }

        public InternalErrorException(string message, Exception innerException)
            : base(message, innerException) { }

        internal InternalErrorException(string format, params object[] args)
            : base(format, args) { }
    }
}
