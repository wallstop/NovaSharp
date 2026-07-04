namespace NovaSharp
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Public Lua value wrapper. This is a facade over <see cref="DynValue"/> until the VM-native
    /// value type lands.
    /// </summary>
    public readonly struct LuaValue : IEquatable<LuaValue>
    {
        private readonly DynValue _value;
        private readonly LuaEngine _owner;

        internal LuaValue(LuaEngine owner, DynValue value)
        {
            _owner = owner;
            _value = value;
        }

        /// <summary>
        /// Gets the nil value.
        /// </summary>
        public static LuaValue Nil => default;

        /// <summary>
        /// Gets the wrapped value kind.
        /// </summary>
        public LuaKind Kind
        {
            get
            {
                DynValue value = GetValueOrNil();
                switch (value.Type)
                {
                    case DataType.Boolean:
                        return LuaKind.Boolean;
                    case DataType.Number:
                        return value.IsInteger ? LuaKind.Integer : LuaKind.Float;
                    case DataType.String:
                        return LuaKind.String;
                    case DataType.Function:
                    case DataType.ClrFunction:
                        return LuaKind.Function;
                    case DataType.Table:
                        return LuaKind.Table;
                    case DataType.Tuple:
                        return LuaKind.Tuple;
                    case DataType.UserData:
                        return LuaKind.UserData;
                    case DataType.Thread:
                        return LuaKind.Thread;
                    case DataType.Nil:
                    case DataType.Void:
                    case DataType.TailCallRequest:
                    case DataType.YieldRequest:
                    default:
                        return LuaKind.Nil;
                }
            }
        }

        /// <summary>
        /// Gets whether this value is nil or no value.
        /// </summary>
        public bool IsNil => GetValueOrNil().IsNil();

        /// <summary>
        /// Gets whether this value is a number.
        /// </summary>
        public bool IsNumber
        {
            get { return GetValueOrNil().Type == DataType.Number; }
        }

        /// <summary>
        /// Gets whether this value is a string.
        /// </summary>
        public bool IsString
        {
            get { return GetValueOrNil().Type == DataType.String; }
        }

        /// <summary>
        /// Gets whether this value is a table.
        /// </summary>
        public bool IsTable
        {
            get { return GetValueOrNil().Type == DataType.Table; }
        }

        /// <summary>
        /// Gets whether this value is callable directly.
        /// </summary>
        public bool IsFunction
        {
            get
            {
                DynValue value = GetValueOrNil();
                return value.Type == DataType.Function || value.Type == DataType.ClrFunction;
            }
        }

        /// <summary>
        /// Gets the Lua number as a double.
        /// </summary>
        public double AsNumber()
        {
            DynValue value = GetValueOrNil();
            if (value.Type != DataType.Number)
            {
                throw NewKindException(nameof(AsNumber), "Number", Kind);
            }

            return value.Number;
        }

        /// <summary>
        /// Gets the Lua number as a 64-bit integer.
        /// </summary>
        public long AsInteger()
        {
            DynValue value = GetValueOrNil();
            if (value.Type != DataType.Number || !value.IsInteger)
            {
                throw NewKindException(nameof(AsInteger), LuaKind.Integer, Kind);
            }

            return value.LuaNumber.AsInteger;
        }

        /// <summary>
        /// Gets the Lua value as a string.
        /// </summary>
        public string AsString()
        {
            DynValue value = RequireType(DataType.String, nameof(AsString));
            return value.String;
        }

        /// <summary>
        /// Gets the Lua value as a Boolean.
        /// </summary>
        public bool AsBoolean()
        {
            DynValue value = RequireType(DataType.Boolean, nameof(AsBoolean));
            return value.Boolean;
        }

        /// <summary>
        /// Gets the Lua value as a table wrapper.
        /// </summary>
        public LuaTable AsTable()
        {
            DynValue value = RequireType(DataType.Table, nameof(AsTable));
            return new LuaTable(GetOwnerOrThrow(), value.Table);
        }

        /// <summary>
        /// Gets the Lua value as a function wrapper.
        /// </summary>
        public LuaFunction AsFunction()
        {
            DynValue value = GetValueOrNil();
            if (value.Type != DataType.Function && value.Type != DataType.ClrFunction)
            {
                throw NewKindException(nameof(AsFunction), LuaKind.Function, Kind);
            }

            return new LuaFunction(GetOwnerOrThrow(), value);
        }

        /// <summary>
        /// Gets the Lua value as a coroutine wrapper.
        /// </summary>
        public LuaCoroutine AsCoroutine()
        {
            DynValue value = RequireType(DataType.Thread, nameof(AsCoroutine));
            return new LuaCoroutine(GetOwnerOrThrow(), value);
        }

        /// <summary>
        /// Gets the Lua tuple values.
        /// </summary>
        public LuaValue[] AsTuple()
        {
            DynValue value = RequireType(DataType.Tuple, nameof(AsTuple));
            LuaEngine tupleOwner = GetOwnerOrThrow();
            DynValue[] tuple = value.Tuple;
            LuaValue[] values = new LuaValue[tuple.Length];
            for (int i = 0; i < tuple.Length; i++)
            {
                LuaEngine owner = RequiresOwner(tuple[i]) ? tupleOwner : null;
                values[i] = new LuaValue(owner, tuple[i]);
            }

            return values;
        }

        /// <summary>
        /// Reads the value as a CLR type through the existing converter pipeline.
        /// </summary>
        public T Read<T>()
        {
            try
            {
                DynValue value = GetValueOrNil();
                if (RequiresOwner(value))
                {
                    GetOwnerOrThrow();
                }

                return value.ToObject<T>();
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Attempts to read the value as a CLR type through the existing converter pipeline.
        /// </summary>
        public bool TryRead<T>(out T value)
        {
            try
            {
                value = Read<T>();
                return true;
            }
            catch (InvalidCastException)
            {
                value = default(T);
                return false;
            }
            catch (LuaException)
            {
                value = default(T);
                return false;
            }
        }

        /// <summary>
        /// Alternate named constructor for Boolean implicit conversion.
        /// </summary>
        public static LuaValue FromBoolean(bool value)
        {
            return new LuaValue(null, DynValue.FromBoolean(value));
        }

        /// <summary>
        /// Alternate named constructor for integer implicit conversion.
        /// </summary>
        public static LuaValue FromInteger(long value)
        {
            return new LuaValue(null, DynValue.FromInteger(value));
        }

        /// <summary>
        /// Alternate named constructor for number implicit conversion.
        /// </summary>
        public static LuaValue FromNumber(double value)
        {
            return new LuaValue(null, DynValue.FromNumber(value));
        }

        /// <summary>
        /// Alternate named constructor for string implicit conversion.
        /// </summary>
        public static LuaValue FromString(string value)
        {
            return new LuaValue(null, value == null ? DynValue.Nil : DynValue.NewString(value));
        }

        /// <summary>
        /// Converts a Boolean to a Lua value.
        /// </summary>
        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "FromBoolean is the named alternate."
        )]
        public static implicit operator LuaValue(bool value)
        {
            return FromBoolean(value);
        }

        /// <summary>
        /// Converts a 32-bit integer to a Lua value.
        /// </summary>
        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "FromInteger is the named alternate."
        )]
        public static implicit operator LuaValue(int value)
        {
            return FromInteger(value);
        }

        /// <summary>
        /// Converts a 64-bit integer to a Lua value.
        /// </summary>
        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "FromInteger is the named alternate."
        )]
        public static implicit operator LuaValue(long value)
        {
            return FromInteger(value);
        }

        /// <summary>
        /// Converts a double-precision number to a Lua value.
        /// </summary>
        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "FromNumber is the named alternate."
        )]
        public static implicit operator LuaValue(double value)
        {
            return FromNumber(value);
        }

        /// <summary>
        /// Converts a string to a Lua value.
        /// </summary>
        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "FromString is the named alternate."
        )]
        public static implicit operator LuaValue(string value)
        {
            return FromString(value);
        }

        /// <inheritdoc />
        public bool Equals(LuaValue other)
        {
            return GetValueOrNil().Equals(other.GetValueOrNil());
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is LuaValue other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return GetValueOrNil().GetHashCode();
        }

        /// <summary>
        /// Determines whether two Lua values are equal under Lua value semantics.
        /// </summary>
        public static bool operator ==(LuaValue left, LuaValue right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two Lua values differ under Lua value semantics.
        /// </summary>
        public static bool operator !=(LuaValue left, LuaValue right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return GetValueOrNil().ToPrintString();
        }

        /// <summary>
        /// Gets the engine that owns this value, or null for scalar literals not yet bound to an engine.
        /// </summary>
        internal LuaEngine Owner => _owner;

        /// <summary>
        /// Gets whether a VM value must stay bound to the engine that produced it.
        /// </summary>
        internal static bool RequiresOwner(DynValue value)
        {
            if (value == null)
            {
                return false;
            }

            switch (value.Type)
            {
                case DataType.ClrFunction:
                case DataType.Function:
                case DataType.Table:
                case DataType.Thread:
                case DataType.Tuple:
                case DataType.UserData:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns the underlying VM value after validating engine ownership.
        /// </summary>
        internal DynValue ToDynValue(LuaEngine owner)
        {
            DynValue value = GetValueOrNil();
            if (_owner != null && !ReferenceEquals(_owner, owner))
            {
                throw new InvalidOperationException(
                    "Lua value belongs to a different LuaEngine instance."
                );
            }

            owner?.ThrowIfDisposed();
            return value;
        }

        private DynValue RequireType(DataType expected, string methodName)
        {
            DynValue value = GetValueOrNil();
            if (value.Type != expected)
            {
                throw NewKindException(methodName, ToKind(expected, value), Kind);
            }

            return value;
        }

        private LuaEngine GetOwnerOrThrow()
        {
            if (_owner == null)
            {
                throw new InvalidOperationException(
                    "Lua value is not owned by a LuaEngine instance."
                );
            }

            _owner.ThrowIfDisposed();
            return _owner;
        }

        private DynValue GetValueOrNil()
        {
            return _value ?? DynValue.Nil;
        }

        private static InvalidOperationException NewKindException(
            string methodName,
            LuaKind expected,
            LuaKind actual
        )
        {
            return NewKindException(methodName, expected.ToString(), actual);
        }

        private static InvalidOperationException NewKindException(
            string methodName,
            string expected,
            LuaKind actual
        )
        {
            return new InvalidOperationException(
                string.Concat(methodName, " requires ", expected, " but found ", actual, ".")
            );
        }

        private static LuaKind ToKind(DataType expected, DynValue value)
        {
            if (expected == DataType.Number)
            {
                return value.IsInteger ? LuaKind.Integer : LuaKind.Float;
            }

            switch (expected)
            {
                case DataType.Boolean:
                    return LuaKind.Boolean;
                case DataType.String:
                    return LuaKind.String;
                case DataType.Function:
                case DataType.ClrFunction:
                    return LuaKind.Function;
                case DataType.Table:
                    return LuaKind.Table;
                case DataType.Tuple:
                    return LuaKind.Tuple;
                case DataType.UserData:
                    return LuaKind.UserData;
                case DataType.Thread:
                    return LuaKind.Thread;
                case DataType.Nil:
                case DataType.Void:
                case DataType.TailCallRequest:
                case DataType.YieldRequest:
                default:
                    return LuaKind.Nil;
            }
        }
    }
}
