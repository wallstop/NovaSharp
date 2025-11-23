namespace NovaSharp.Interpreter.Interop
{
    using System;

    /// <summary>
    /// Helps identifying a reflection special name
    /// </summary>
    public enum ReflectionSpecialNameType
    {
        [Obsolete("Use a specific ReflectionSpecialNameType.", false)]
        Unknown = 0,
        IndexGetter = 1,
        IndexSetter = 2,
        ImplicitCast = 3,
        ExplicitCast = 4,
        OperatorTrue = 5,
        OperatorFalse = 6,
        PropertyGetter = 7,
        PropertySetter = 8,
        AddEvent = 9,
        RemoveEvent = 10,
        OperatorAdd = 11,
        OperatorAnd = 12,
        OperatorOr = 13,
        OperatorDec = 14,
        OperatorDiv = 15,
        OperatorEq = 16,
        OperatorXor = 17,
        OperatorGt = 18,
        OperatorGte = 19,
        OperatorInc = 20,
        OperatorNeq = 21,
        OperatorLt = 22,
        OperatorLte = 23,
        OperatorNot = 24,
        OperatorMod = 25,
        OperatorMul = 26,
        OperatorCompl = 27,
        OperatorSub = 28,
        OperatorNeg = 29,
        OperatorUnaryPlus = 30,
    }

    /// <summary>
    /// Class helping identifying special names found with reflection
    /// </summary>
    public struct ReflectionSpecialName : IEquatable<ReflectionSpecialName>
    {
        /// <summary>
        /// Gets the category of special name discovered via reflection.
        /// </summary>
        public ReflectionSpecialNameType Type { get; private set; }

        /// <summary>
        /// Gets the optional argument associated with the special name (property name, operator token, etc.).
        /// </summary>
        public string Argument { get; private set; }

        /// <summary>
        /// Initializes a new instance with the specified type/argument.
        /// </summary>
        public ReflectionSpecialName(ReflectionSpecialNameType type, string argument = null)
            : this()
        {
            Type = type;
            Argument = argument;
        }

        /// <summary>
        /// Parses a CLR special-name string (e.g., <c>op_Addition</c> or <c>get_Item</c>).
        /// </summary>
        public ReflectionSpecialName(string name)
            : this()
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Special name cannot be null or empty.", nameof(name));
            }

            if (name.Contains('.', StringComparison.Ordinal))
            {
                string[] split = name.Split('.');
                name = split[^1];
            }

            switch (name)
            {
                case "op_Explicit":
                    Type = ReflectionSpecialNameType.ExplicitCast;
                    return;
                case "op_Implicit":
                    Type = ReflectionSpecialNameType.ImplicitCast;
                    return;
                case "set_Item":
                    Type = ReflectionSpecialNameType.IndexSetter;
                    return;
                case "get_Item":
                    Type = ReflectionSpecialNameType.IndexGetter;
                    return;
                case "op_Addition":
                    Type = ReflectionSpecialNameType.OperatorAdd;
                    Argument = "+";
                    return;
                case "op_BitwiseAnd":
                    Type = ReflectionSpecialNameType.OperatorAnd;
                    Argument = "&";
                    return;
                case "op_BitwiseOr":
                    Type = ReflectionSpecialNameType.OperatorOr;
                    Argument = "|";
                    return;
                case "op_Decrement":
                    Type = ReflectionSpecialNameType.OperatorDec;
                    Argument = "--";
                    return;
                case "op_Division":
                    Type = ReflectionSpecialNameType.OperatorDiv;
                    Argument = "/";
                    return;
                case "op_Equality":
                    Type = ReflectionSpecialNameType.OperatorEq;
                    Argument = "==";
                    return;
                case "op_ExclusiveOr":
                    Type = ReflectionSpecialNameType.OperatorXor;
                    Argument = "^";
                    return;
                case "op_False":
                    Type = ReflectionSpecialNameType.OperatorFalse;
                    return;
                case "op_GreaterThan":
                    Type = ReflectionSpecialNameType.OperatorGt;
                    Argument = ">";
                    return;
                case "op_GreaterThanOrEqual":
                    Type = ReflectionSpecialNameType.OperatorGte;
                    Argument = ">=";
                    return;
                case "op_Increment":
                    Type = ReflectionSpecialNameType.OperatorInc;
                    Argument = "++";
                    return;
                case "op_Inequality":
                    Type = ReflectionSpecialNameType.OperatorNeq;
                    Argument = "!=";
                    return;
                case "op_LessThan":
                    Type = ReflectionSpecialNameType.OperatorLt;
                    Argument = "<";
                    return;
                case "op_LessThanOrEqual":
                    Type = ReflectionSpecialNameType.OperatorLte;
                    Argument = "<=";
                    return;
                case "op_LogicalNot":
                    Type = ReflectionSpecialNameType.OperatorNot;
                    Argument = "!";
                    return;
                case "op_Modulus":
                    Type = ReflectionSpecialNameType.OperatorMod;
                    Argument = "%";
                    return;
                case "op_Multiply":
                    Type = ReflectionSpecialNameType.OperatorMul;
                    Argument = "*";
                    return;
                case "op_OnesComplement":
                    Type = ReflectionSpecialNameType.OperatorCompl;
                    Argument = "~";
                    return;
                case "op_Subtraction":
                    Type = ReflectionSpecialNameType.OperatorSub;
                    Argument = "-";
                    return;
                case "op_True":
                    Type = ReflectionSpecialNameType.OperatorTrue;
                    return;
                case "op_UnaryNegation":
                    Type = ReflectionSpecialNameType.OperatorNeg;
                    Argument = "-";
                    return;
                case "op_UnaryPlus":
                    Type = ReflectionSpecialNameType.OperatorUnaryPlus;
                    Argument = "+";
                    return;
            }

            if (name.StartsWith("get_", StringComparison.Ordinal))
            {
                Type = ReflectionSpecialNameType.PropertyGetter;
                Argument = name.Substring(4);
            }
            else if (name.StartsWith("set_", StringComparison.Ordinal))
            {
                Type = ReflectionSpecialNameType.PropertySetter;
                Argument = name.Substring(4);
            }
            else if (name.StartsWith("add_", StringComparison.Ordinal))
            {
                Type = ReflectionSpecialNameType.AddEvent;
                Argument = name.Substring(4);
            }
            else if (name.StartsWith("remove_", StringComparison.Ordinal))
            {
                Type = ReflectionSpecialNameType.RemoveEvent;
                Argument = name.Substring(7);
            }
        }

        /// <inheritdoc />
        public bool Equals(ReflectionSpecialName other)
        {
            return Type == other.Type
                && string.Equals(Argument, other.Argument, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ReflectionSpecialName name && Equals(name);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + Type.GetHashCode();
                hash =
                    (hash * 31)
                    + (Argument != null ? Argument.GetHashCode(StringComparison.Ordinal) : 0);
                return hash;
            }
        }

        /// <summary>
        /// Equality operator for comparing two special names.
        /// </summary>
        public static bool operator ==(ReflectionSpecialName left, ReflectionSpecialName right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for comparing two special names.
        /// </summary>
        public static bool operator !=(ReflectionSpecialName left, ReflectionSpecialName right)
        {
            return !left.Equals(right);
        }
    }
}
