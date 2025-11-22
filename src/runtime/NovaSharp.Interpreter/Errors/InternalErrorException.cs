namespace NovaSharp.Interpreter.Errors
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Exception thrown when an inconsistent state is reached in the interpreter
    /// </summary>
#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    [Serializable]
#endif
    public class InternalErrorException : InterpreterException
    {
        private const string DefaultMessage = "Internal error";

        public InternalErrorException()
            : base(DefaultMessage) { }

        public InternalErrorException(string message)
            : base(NormalizeMessage(message)) { }

        public InternalErrorException(string message, Exception innerException)
            : base(NormalizeMessage(message), innerException) { }

        internal InternalErrorException(string format, params object[] args)
            : base(FormatMessage(format, args)) { }

        private static string NormalizeMessage(string message)
        {
            return string.IsNullOrWhiteSpace(message) ? DefaultMessage : message;
        }

        private static string FormatMessage(string format, object[] args)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return DefaultMessage;
            }

            object[] safeArgs = args ?? Array.Empty<object>();

            if (safeArgs.Length == 0)
            {
                return format;
            }

            return string.Format(CultureInfo.InvariantCulture, format, safeArgs);
        }
    }
}
