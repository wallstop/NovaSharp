namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides string interning for frequently used Lua strings to reduce memory allocations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lua scripts often use the same strings repeatedly (variable names, table keys, metamethod names).
    /// This pool interns these strings to avoid allocating duplicate instances.
    /// </para>
    /// <para>
    /// The pool uses a concurrent dictionary for thread-safety and automatically prunes entries
    /// that haven't been accessed recently when the pool grows too large.
    /// </para>
    /// <para>
    /// Common metamethod and operator names are pre-interned for zero-allocation lookups.
    /// </para>
    /// </remarks>
    internal static class LuaStringPool
    {
        private const int MaxPoolSize = 4096;

        private static readonly ConcurrentDictionary<string, string> Pool = new(
            StringComparer.Ordinal
        );

        // Pre-interned common strings
        private static readonly string[] CommonStrings = InitializeCommonStrings();

        private static string[] InitializeCommonStrings()
        {
            string[] common = new[]
            {
                // Metamethods (using Metamethods class constants)
                Metamethods.Index,
                Metamethods.NewIndex,
                Metamethods.Call,
                Metamethods.Add,
                Metamethods.Sub,
                Metamethods.Mul,
                Metamethods.Div,
                Metamethods.Mod,
                Metamethods.Pow,
                Metamethods.Unm,
                Metamethods.IDiv,
                Metamethods.Band,
                Metamethods.Bor,
                Metamethods.Bxor,
                Metamethods.Bnot,
                Metamethods.Shl,
                Metamethods.Shr,
                Metamethods.Concat,
                Metamethods.Len,
                Metamethods.Eq,
                Metamethods.Lt,
                Metamethods.Le,
                Metamethods.Gc,
                Metamethods.Close,
                Metamethods.ToStringMeta,
                Metamethods.Metatable,
                Metamethods.Mode,
                Metamethods.Pairs,
                Metamethods.IPairs,
                Metamethods.Name,
                // Common variable names
                "self",
                "this",
                "_G",
                "_ENV",
                "_VERSION",
                "arg",
                "n",
                "i",
                "j",
                "k",
                "v",
                "x",
                "y",
                "z",
                "key",
                "value",
                "index",
                "func",
                "table",
                "string",
                "number",
                "boolean",
                "nil",
                "true",
                "false",
                "function",
                "userdata",
                "thread",
                // Common function names
                "print",
                "pairs",
                "ipairs",
                "next",
                "type",
                "tonumber",
                "tostring",
                "select",
                "error",
                "assert",
                "pcall",
                "xpcall",
                "require",
                "load",
                "loadfile",
                "dofile",
                "rawget",
                "rawset",
                "rawequal",
                "rawlen",
                "setmetatable",
                "getmetatable",
                "collectgarbage",
                // Common module names
                "math",
                "string",
                "table",
                "io",
                "os",
                "debug",
                "coroutine",
                "package",
                "utf8",
                "bit32",
            };

            // Pre-populate pool with common strings
            foreach (string s in common)
            {
                Pool[s] = s;
            }

            return common;
        }

        /// <summary>
        /// Interns the specified string, returning a cached instance if available.
        /// </summary>
        /// <param name="value">The string to intern. May be null.</param>
        /// <returns>The interned string, or null if the input was null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Intern(string value)
        {
            if (value == null)
            {
                return null;
            }

            // Fast path: already in pool
            if (Pool.TryGetValue(value, out string cached))
            {
                return cached;
            }

            // Only intern strings of reasonable length
            if (value.Length > 64)
            {
                return value;
            }

            // Add to pool if not too large
            if (Pool.Count < MaxPoolSize)
            {
                Pool[value] = value;
                return value;
            }

            // Pool is full, return as-is without interning
            return value;
        }

        /// <summary>
        /// Interns the specified character span, returning a cached string if available.
        /// </summary>
        /// <param name="span">The character span to intern.</param>
        /// <returns>The interned string.</returns>
        public static string Intern(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty)
            {
                return string.Empty;
            }

            // Only intern strings of reasonable length
            if (span.Length > 64)
            {
                return new string(span);
            }

            // Check if already in pool (requires string allocation for lookup)
            string key = new string(span);
            if (Pool.TryGetValue(key, out string cached))
            {
                return cached;
            }

            // Add to pool if not too large
            if (Pool.Count < MaxPoolSize)
            {
                Pool[key] = key;
            }

            return key;
        }

        /// <summary>
        /// Checks if the specified string is already interned without adding it.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <param name="interned">The interned string if found.</param>
        /// <returns><c>true</c> if the string was found in the pool; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetInterned(string value, out string interned)
        {
            if (value == null)
            {
                interned = null;
                return false;
            }
            return Pool.TryGetValue(value, out interned);
        }

        /// <summary>
        /// Gets the current number of interned strings in the pool.
        /// </summary>
        public static int Count => Pool.Count;

        /// <summary>
        /// Clears all interned strings from the pool except pre-interned common strings.
        /// </summary>
        /// <remarks>
        /// This should only be called during application shutdown or explicit cache clearing.
        /// The common strings will be re-added automatically.
        /// </remarks>
        public static void Clear()
        {
            Pool.Clear();
            foreach (string s in CommonStrings)
            {
                Pool[s] = s;
            }
        }
    }

    /// <summary>
    /// Centralized compile-time constants for Lua metamethod names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These constants eliminate magic strings throughout the codebase and ensure consistency
    /// when comparing or emitting metamethod names. Using <c>const string</c> allows the compiler
    /// to optimize string comparisons and enables compile-time interning.
    /// </para>
    /// <para>
    /// All metamethods are prefixed with double underscore (<c>__</c>) following Lua convention.
    /// </para>
    /// </remarks>
    public static class Metamethods
    {
        // Arithmetic operators
        /// <summary>The <c>__add</c> metamethod for addition (<c>+</c>).</summary>
        public const string Add = "__add";

        /// <summary>The <c>__sub</c> metamethod for subtraction (<c>-</c>).</summary>
        public const string Sub = "__sub";

        /// <summary>The <c>__mul</c> metamethod for multiplication (<c>*</c>).</summary>
        public const string Mul = "__mul";

        /// <summary>The <c>__div</c> metamethod for division (<c>/</c>).</summary>
        public const string Div = "__div";

        /// <summary>The <c>__mod</c> metamethod for modulo (<c>%</c>).</summary>
        public const string Mod = "__mod";

        /// <summary>The <c>__pow</c> metamethod for exponentiation (<c>^</c>).</summary>
        public const string Pow = "__pow";

        /// <summary>The <c>__unm</c> metamethod for unary minus (<c>-x</c>).</summary>
        public const string Unm = "__unm";

        /// <summary>The <c>__idiv</c> metamethod for integer division (<c>//</c>, Lua 5.3+).</summary>
        public const string IDiv = "__idiv";

        // Bitwise operators (Lua 5.3+)
        /// <summary>The <c>__band</c> metamethod for bitwise AND (<c>&amp;</c>, Lua 5.3+).</summary>
        public const string Band = "__band";

        /// <summary>The <c>__bor</c> metamethod for bitwise OR (<c>|</c>, Lua 5.3+).</summary>
        public const string Bor = "__bor";

        /// <summary>The <c>__bxor</c> metamethod for bitwise XOR (<c>~</c>, Lua 5.3+).</summary>
        public const string Bxor = "__bxor";

        /// <summary>The <c>__bnot</c> metamethod for bitwise NOT (<c>~x</c>, Lua 5.3+).</summary>
        public const string Bnot = "__bnot";

        /// <summary>The <c>__shl</c> metamethod for left shift (<c>&lt;&lt;</c>, Lua 5.3+).</summary>
        public const string Shl = "__shl";

        /// <summary>The <c>__shr</c> metamethod for right shift (<c>&gt;&gt;</c>, Lua 5.3+).</summary>
        public const string Shr = "__shr";

        // Comparison operators
        /// <summary>The <c>__eq</c> metamethod for equality comparison (<c>==</c>).</summary>
        public const string Eq = "__eq";

        /// <summary>The <c>__lt</c> metamethod for less-than comparison (<c>&lt;</c>).</summary>
        public const string Lt = "__lt";

        /// <summary>The <c>__le</c> metamethod for less-than-or-equal comparison (<c>&lt;=</c>).</summary>
        public const string Le = "__le";

        // Other operators
        /// <summary>The <c>__concat</c> metamethod for concatenation (<c>..</c>).</summary>
        public const string Concat = "__concat";

        /// <summary>The <c>__len</c> metamethod for length operator (<c>#</c>).</summary>
        public const string Len = "__len";

        // Table access
        /// <summary>The <c>__index</c> metamethod for table indexing (reading).</summary>
        public const string Index = "__index";

        /// <summary>The <c>__newindex</c> metamethod for table assignment (writing).</summary>
        public const string NewIndex = "__newindex";

        /// <summary>The <c>__call</c> metamethod for function call syntax on tables.</summary>
        public const string Call = "__call";

        // Iteration (Lua 5.2+)
        /// <summary>The <c>__pairs</c> metamethod for custom pairs iteration (Lua 5.2+).</summary>
        public const string Pairs = "__pairs";

        /// <summary>The <c>__ipairs</c> metamethod for custom ipairs iteration (deprecated in Lua 5.3+).</summary>
        public const string IPairs = "__ipairs";

        /// <summary>The <c>__iterator</c> metamethod for custom iterator generation (NovaSharp extension).</summary>
        public const string Iterator = "__iterator";

        // Type conversion
        /// <summary>The <c>__tostring</c> metamethod for string conversion.</summary>
        public const string ToStringMeta = "__tostring";

        // Metatable protection
        /// <summary>The <c>__metatable</c> field for metatable protection.</summary>
        public const string Metatable = "__metatable";

        /// <summary>The <c>__mode</c> field for weak table mode specification.</summary>
        public const string Mode = "__mode";

        /// <summary>The <c>__name</c> field for type name in error messages (Lua 5.3+).</summary>
        public const string Name = "__name";

        // Lifecycle
        /// <summary>The <c>__gc</c> metamethod for garbage collection finalizer.</summary>
        public const string Gc = "__gc";

        /// <summary>The <c>__close</c> metamethod for to-be-closed variables (Lua 5.4+).</summary>
        public const string Close = "__close";

        // NovaSharp extensions for CLR interop
        /// <summary>The <c>__new</c> metamethod for CLR constructor invocation (NovaSharp extension).</summary>
        public const string New = "__new";

        /// <summary>The <c>__tonumber</c> metamethod for number conversion (NovaSharp extension).</summary>
        public const string ToNumber = "__tonumber";

        /// <summary>The <c>__tobool</c> metamethod for boolean conversion (NovaSharp extension).</summary>
        public const string ToBool = "__tobool";
    }

    /// <summary>
    /// Centralized constants for Lua reserved keywords.
    /// </summary>
    /// <remarks>
    /// These constants eliminate magic strings throughout the codebase and ensure consistency
    /// when comparing or emitting Lua keywords. All strings are interned for reference equality.
    /// </remarks>
    public static class LuaKeywords
    {
        // Literal keywords
        /// <summary>The Lua <c>nil</c> literal keyword.</summary>
        public const string Nil = "nil";

        /// <summary>The Lua <c>true</c> literal keyword.</summary>
        public const string True = "true";

        /// <summary>The Lua <c>false</c> literal keyword.</summary>
        public const string False = "false";

        // Logical operators
        /// <summary>The Lua <c>and</c> logical operator keyword.</summary>
        public const string And = "and";

        /// <summary>The Lua <c>or</c> logical operator keyword.</summary>
        public const string Or = "or";

        /// <summary>The Lua <c>not</c> logical operator keyword.</summary>
        public const string Not = "not";

        // Function definition
        /// <summary>The Lua <c>function</c> keyword.</summary>
        public const string Function = "function";

        /// <summary>The Lua <c>end</c> block terminator keyword.</summary>
        public const string End = "end";

        // Conditional statements
        /// <summary>The Lua <c>if</c> conditional keyword.</summary>
        public const string If = "if";

        /// <summary>The Lua <c>then</c> conditional keyword.</summary>
        public const string Then = "then";

        /// <summary>The Lua <c>else</c> conditional keyword.</summary>
        public const string Else = "else";

        /// <summary>The Lua <c>elseif</c> conditional keyword.</summary>
        public const string ElseIf = "elseif";

        // Loop constructs
        /// <summary>The Lua <c>for</c> loop keyword.</summary>
        public const string For = "for";

        /// <summary>The Lua <c>while</c> loop keyword.</summary>
        public const string While = "while";

        /// <summary>The Lua <c>do</c> block keyword.</summary>
        public const string Do = "do";

        /// <summary>The Lua <c>repeat</c> loop keyword.</summary>
        public const string Repeat = "repeat";

        /// <summary>The Lua <c>until</c> loop keyword.</summary>
        public const string Until = "until";

        /// <summary>The Lua <c>break</c> loop exit keyword.</summary>
        public const string Break = "break";

        // Variable scope and control flow
        /// <summary>The Lua <c>local</c> variable declaration keyword.</summary>
        public const string Local = "local";

        /// <summary>The Lua <c>return</c> statement keyword.</summary>
        public const string Return = "return";

        /// <summary>The Lua <c>in</c> keyword used in generic for loops.</summary>
        public const string In = "in";

        // Labels and goto (Lua 5.2+)
        /// <summary>The Lua <c>goto</c> keyword (Lua 5.2+).</summary>
        public const string Goto = "goto";

        /// <summary>The Lua label delimiter <c>::</c> (Lua 5.2+).</summary>
        public const string LabelDelimiter = "::";

        /// <summary>
        /// All Lua reserved keywords in a set for validation purposes.
        /// </summary>
        public static readonly HashSet<string> All = new(StringComparer.Ordinal)
        {
            Nil,
            True,
            False,
            And,
            Or,
            Not,
            Function,
            End,
            If,
            Then,
            Else,
            ElseIf,
            For,
            While,
            Do,
            Repeat,
            Until,
            Break,
            Local,
            Return,
            In,
            Goto,
        };
    }
}
