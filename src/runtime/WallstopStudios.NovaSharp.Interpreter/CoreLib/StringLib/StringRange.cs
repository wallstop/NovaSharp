namespace WallstopStudios.NovaSharp.Interpreter.CoreLib.StringLib
{
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Represents a Lua-style string range (1-based, inclusive, supports negative indices) and
    /// provides helpers to apply it against .NET strings.
    /// </summary>
    internal class StringRange
    {
        /// <summary>
        /// Gets or sets the inclusive starting index (Lua 1-based; negatives count from the end).
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Gets or sets the inclusive ending index using Lua semantics.
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// Initializes a range with zero-based defaults (useful for deferred assignment).
        /// </summary>
        public StringRange()
        {
            Start = 0;
            End = 0;
        }

        /// <summary>
        /// Initializes a range with the provided Lua-style start/end values.
        /// </summary>
        /// <param name="start">Inclusive starting index (Lua semantics).</param>
        /// <param name="end">Inclusive ending index.</param>
        public StringRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Creates a <see cref="StringRange"/> from Lua substring arguments, defaulting missing
        /// values according to the Lua spec.
        /// </summary>
        /// <param name="start">Lua `i` argument (can be nil).</param>
        /// <param name="end">Lua `j` argument (can be nil).</param>
        /// <param name="defaultEnd">Optional default for `j` when omitted.</param>
        /// <returns>Constructed range.</returns>
        public static StringRange FromLuaRange(DynValue start, DynValue end, int? defaultEnd = null)
        {
            int i = start.IsNil() ? 1 : (int)start.Number;
            int j = end.IsNil() ? (defaultEnd ?? i) : (int)end.Number;

            return new StringRange(i, j);
        }

        // Returns the substring of s that starts at i and continues until j; i and j can be negative.
        // If, after the translation of negative indices, i is less than 1, it is corrected to 1.
        // If j is greater than the string length, it is corrected to that length.
        // If, after these corrections, i is greater than j, the function returns the empty string.
        /// <summary>
        /// Applies the range to a string using Lua's substring semantics (1-based, inclusive, clamped).
        /// </summary>
        /// <param name="value">Target string.</param>
        /// <returns>Substring defined by the range.</returns>
        public string ApplyToString(string value)
        {
            int i = Start < 0 ? Start + value.Length + 1 : Start;
            int j = End < 0 ? End + value.Length + 1 : End;

            if (i < 1)
            {
                i = 1;
            }

            if (j > value.Length)
            {
                j = value.Length;
            }

            if (i > j)
            {
                return string.Empty;
            }

            return value.Substring(i - 1, j - i + 1);
        }

        /// <summary>
        /// Computes the Lua-style length for the range (End - Start + 1).
        /// </summary>
        /// <returns>Length represented by this range.</returns>
        public int Length()
        {
            return (End - Start) + 1;
        }
    }
}
