namespace NovaSharp
{
    using System;

    /// <summary>
    /// Marks a partial CLR type as a Lua-bindable object for generated interop.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class LuaObjectAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuaObjectAttribute"/> class.
        /// </summary>
        public LuaObjectAttribute() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaObjectAttribute"/> class.
        /// </summary>
        public LuaObjectAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Lua object name cannot be empty.", nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// Gets the Lua-visible object name, or null to use the CLR type name.
        /// </summary>
        public string Name { get; }
    }

    /// <summary>
    /// Marks a CLR member as exported to Lua by generated interop.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field,
        Inherited = false
    )]
    public sealed class LuaMemberAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuaMemberAttribute"/> class.
        /// </summary>
        public LuaMemberAttribute() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaMemberAttribute"/> class.
        /// </summary>
        public LuaMemberAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Lua member name cannot be empty.", nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// Gets the Lua-visible member name, or null to use the CLR member name.
        /// </summary>
        public string Name { get; }
    }

    /// <summary>
    /// Marks a CLR method as a Lua metamethod for generated interop.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class LuaMetamethodAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuaMetamethodAttribute"/> class.
        /// </summary>
        public LuaMetamethodAttribute(LuaMetamethodKind kind)
        {
            if (kind == LuaMetamethodKind.Custom)
            {
                throw new ArgumentException(
                    "Use the string constructor for custom Lua metamethod names.",
                    nameof(kind)
                );
            }

            Kind = kind;
            Name = ToMetamethodName(kind);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaMetamethodAttribute"/> class.
        /// </summary>
        public LuaMetamethodAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Lua metamethod name cannot be empty.", nameof(name));
            }

            Kind = LuaMetamethodKind.Custom;
            Name = name;
        }

        /// <summary>
        /// Gets the standard metamethod kind, or <see cref="LuaMetamethodKind.Custom"/> for custom names.
        /// </summary>
        public LuaMetamethodKind Kind { get; }

        /// <summary>
        /// Gets the Lua metamethod name.
        /// </summary>
        public string Name { get; }

        private static string ToMetamethodName(LuaMetamethodKind kind)
        {
            switch (kind)
            {
                case LuaMetamethodKind.Add:
                    return "__add";
                case LuaMetamethodKind.Subtract:
                    return "__sub";
                case LuaMetamethodKind.Multiply:
                    return "__mul";
                case LuaMetamethodKind.Modulo:
                    return "__mod";
                case LuaMetamethodKind.Power:
                    return "__pow";
                case LuaMetamethodKind.Divide:
                    return "__div";
                case LuaMetamethodKind.FloorDivide:
                    return "__idiv";
                case LuaMetamethodKind.BitwiseAnd:
                    return "__band";
                case LuaMetamethodKind.BitwiseOr:
                    return "__bor";
                case LuaMetamethodKind.BitwiseXor:
                    return "__bxor";
                case LuaMetamethodKind.BitwiseNot:
                    return "__bnot";
                case LuaMetamethodKind.ShiftLeft:
                    return "__shl";
                case LuaMetamethodKind.ShiftRight:
                    return "__shr";
                case LuaMetamethodKind.UnaryMinus:
                    return "__unm";
                case LuaMetamethodKind.Concat:
                    return "__concat";
                case LuaMetamethodKind.Length:
                    return "__len";
                case LuaMetamethodKind.Equal:
                    return "__eq";
                case LuaMetamethodKind.LessThan:
                    return "__lt";
                case LuaMetamethodKind.LessThanOrEqual:
                    return "__le";
                case LuaMetamethodKind.Index:
                    return "__index";
                case LuaMetamethodKind.NewIndex:
                    return "__newindex";
                case LuaMetamethodKind.Call:
                    return "__call";
                case LuaMetamethodKind.Close:
                    return "__close";
                case LuaMetamethodKind.GarbageCollect:
                    return "__gc";
                case LuaMetamethodKind.Mode:
                    return "__mode";
                case LuaMetamethodKind.Name:
                    return "__name";
                case LuaMetamethodKind.Pairs:
                    return "__pairs";
                case LuaMetamethodKind.ToString:
                    return "__tostring";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }

    /// <summary>
    /// Standard Lua metamethod names supported by generated interop.
    /// </summary>
    public enum LuaMetamethodKind
    {
        /// <summary>
        /// A custom metamethod name supplied by string.
        /// </summary>
        Custom,

        /// <summary>
        /// The __add metamethod.
        /// </summary>
        Add,

        /// <summary>
        /// The __sub metamethod.
        /// </summary>
        Subtract,

        /// <summary>
        /// The __mul metamethod.
        /// </summary>
        Multiply,

        /// <summary>
        /// The __mod metamethod.
        /// </summary>
        Modulo,

        /// <summary>
        /// The __pow metamethod.
        /// </summary>
        Power,

        /// <summary>
        /// The __div metamethod.
        /// </summary>
        Divide,

        /// <summary>
        /// The __idiv metamethod.
        /// </summary>
        FloorDivide,

        /// <summary>
        /// The __band metamethod.
        /// </summary>
        BitwiseAnd,

        /// <summary>
        /// The __bor metamethod.
        /// </summary>
        BitwiseOr,

        /// <summary>
        /// The __bxor metamethod.
        /// </summary>
        BitwiseXor,

        /// <summary>
        /// The __bnot metamethod.
        /// </summary>
        BitwiseNot,

        /// <summary>
        /// The __shl metamethod.
        /// </summary>
        ShiftLeft,

        /// <summary>
        /// The __shr metamethod.
        /// </summary>
        ShiftRight,

        /// <summary>
        /// The __unm metamethod.
        /// </summary>
        UnaryMinus,

        /// <summary>
        /// The __concat metamethod.
        /// </summary>
        Concat,

        /// <summary>
        /// The __len metamethod.
        /// </summary>
        Length,

        /// <summary>
        /// The __eq metamethod.
        /// </summary>
        Equal,

        /// <summary>
        /// The __lt metamethod.
        /// </summary>
        LessThan,

        /// <summary>
        /// The __le metamethod.
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// The __index metamethod.
        /// </summary>
        Index,

        /// <summary>
        /// The __newindex metamethod.
        /// </summary>
        NewIndex,

        /// <summary>
        /// The __call metamethod.
        /// </summary>
        Call,

        /// <summary>
        /// The __close metamethod.
        /// </summary>
        Close,

        /// <summary>
        /// The __gc metamethod.
        /// </summary>
        GarbageCollect,

        /// <summary>
        /// The __mode metamethod.
        /// </summary>
        Mode,

        /// <summary>
        /// The __name metamethod.
        /// </summary>
        Name,

        /// <summary>
        /// The __pairs metamethod.
        /// </summary>
        Pairs,

        /// <summary>
        /// The __tostring metamethod.
        /// </summary>
        ToString,
    }

    /// <summary>
    /// Excludes a CLR type or member from generated Lua interop.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class
            | AttributeTargets.Struct
            | AttributeTargets.Enum
            | AttributeTargets.Constructor
            | AttributeTargets.Method
            | AttributeTargets.Property
            | AttributeTargets.Field,
        Inherited = false
    )]
    public sealed class LuaIgnoreAttribute : Attribute { }
}
