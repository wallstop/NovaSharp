namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Threading;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Modding;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Registers custom TryFormat handlers for frequently-used enum types with ZString
    /// to avoid ToString() allocations when appending enum values to Utf16ValueStringBuilder.
    /// </summary>
    internal static class ZStringEnumFormatters
    {
        private static int Initialized;

        /// <summary>
        /// Registers TryFormat handlers for all supported enum types.
        /// This method is thread-safe and will only execute once.
        /// </summary>
        public static void Initialize()
        {
            if (Interlocked.CompareExchange(ref Initialized, 1, 0) != 0)
            {
                return;
            }

            Utf16ValueStringBuilder.RegisterTryFormat<DataType>(TryFormatDataType);
            Utf16ValueStringBuilder.RegisterTryFormat<TokenType>(TryFormatTokenType);
            Utf16ValueStringBuilder.RegisterTryFormat<OpCode>(TryFormatOpCode);
            Utf16ValueStringBuilder.RegisterTryFormat<SymbolRefType>(TryFormatSymbolRefType);
            Utf16ValueStringBuilder.RegisterTryFormat<ModLoadState>(TryFormatModLoadState);
            Utf16ValueStringBuilder.RegisterTryFormat<DebuggerAction.ActionType>(
                TryFormatDebuggerActionType
            );
        }

        private static bool TryFormatDataType(
            DataType value,
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format
        )
        {
            string name = value.ToLuaDebuggerString();
            if (name.Length <= destination.Length)
            {
                name.AsSpan().CopyTo(destination);
                charsWritten = name.Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        private static bool TryFormatTokenType(
            TokenType value,
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format
        )
        {
            string name = TokenTypeStrings.GetName(value);
            if (name.Length <= destination.Length)
            {
                name.AsSpan().CopyTo(destination);
                charsWritten = name.Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        private static bool TryFormatOpCode(
            OpCode value,
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format
        )
        {
            string name = OpCodeStrings.GetName(value);
            if (name.Length <= destination.Length)
            {
                name.AsSpan().CopyTo(destination);
                charsWritten = name.Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        private static bool TryFormatSymbolRefType(
            SymbolRefType value,
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format
        )
        {
            string name = SymbolRefTypeStrings.GetName(value);
            if (name.Length <= destination.Length)
            {
                name.AsSpan().CopyTo(destination);
                charsWritten = name.Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        private static bool TryFormatModLoadState(
            ModLoadState value,
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format
        )
        {
            string name = ModLoadStateStrings.GetName(value);
            if (name.Length <= destination.Length)
            {
                name.AsSpan().CopyTo(destination);
                charsWritten = name.Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        private static bool TryFormatDebuggerActionType(
            DebuggerAction.ActionType value,
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format
        )
        {
            string name = DebuggerActionTypeStrings.GetName(value);
            if (name.Length <= destination.Length)
            {
                name.AsSpan().CopyTo(destination);
                charsWritten = name.Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }
    }
}
