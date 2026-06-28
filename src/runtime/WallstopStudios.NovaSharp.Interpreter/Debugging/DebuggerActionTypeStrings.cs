namespace WallstopStudios.NovaSharp.Interpreter.Debugging
{
    using System;

    /// <summary>
    /// Provides cached string representations for <see cref="DebuggerAction.ActionType"/> enum values
    /// to avoid allocations from ToString() calls.
    /// </summary>
    internal static class DebuggerActionTypeStrings
    {
        private static readonly string[] Names =
        {
            "Unknown", // 0
            "ByteCodeStepIn", // 1
            "ByteCodeStepOver", // 2
            "ByteCodeStepOut", // 3
            "StepIn", // 4
            "StepOver", // 5
            "StepOut", // 6
            "Run", // 7
            "ToggleBreakpoint", // 8
            "SetBreakpoint", // 9
            "ClearBreakpoint", // 10
            "ResetBreakpoints", // 11
            "Refresh", // 12
            "HardRefresh", // 13
            "None", // 14
        };

        /// <summary>
        /// Gets the cached string name for the specified action type.
        /// </summary>
        /// <param name="action">The action type to get the name for.</param>
        /// <returns>The string representation of the action type.</returns>
        public static string GetName(DebuggerAction.ActionType action)
        {
            int index = (int)action;
            if (index >= 0 && index < Names.Length)
            {
                return Names[index];
            }
            return action.ToString();
        }
    }
}
