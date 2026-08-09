namespace NovaSharp
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Public Lua value wrapper over the VM-native <see cref="DynValue"/> value type.
    /// </summary>
    public readonly struct LuaValue : IEquatable<LuaValue>
    {
        private readonly DynValue _value;

        internal LuaValue(Script ownerScript, DynValue value)
        {
            Script intrinsicOwner = value.GetOwnerScript();
            if (
                intrinsicOwner != null
                && ownerScript != null
                && !ReferenceEquals(intrinsicOwner, ownerScript)
            )
            {
                throw new InvalidOperationException(
                    "Lua value belongs to a different LuaEngine instance."
                );
            }

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
            get { return GetValueOrNil().Kind; }
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
            get { return GetValueOrNil().IsNumber; }
        }

        /// <summary>
        /// Gets whether this value is a string.
        /// </summary>
        public bool IsString
        {
            get { return GetValueOrNil().IsString; }
        }

        /// <summary>
        /// Gets whether this value is a table.
        /// </summary>
        public bool IsTable
        {
            get { return GetValueOrNil().IsTable; }
        }

        /// <summary>
        /// Gets whether this value is callable directly.
        /// </summary>
        public bool IsFunction
        {
            get { return GetValueOrNil().IsFunction; }
        }

        /// <summary>
        /// Gets the Lua number as a double.
        /// </summary>
        public double AsNumber()
        {
            return GetValueOrNil().AsNumber();
        }

        /// <summary>
        /// Gets the Lua number as a 64-bit integer.
        /// </summary>
        public long AsInteger()
        {
            return GetValueOrNil().AsInteger();
        }

        /// <summary>
        /// Gets the Lua value as a string.
        /// </summary>
        public string AsString()
        {
            return GetValueOrNil().AsString();
        }

        /// <summary>
        /// Gets the Lua value as a Boolean.
        /// </summary>
        public bool AsBoolean()
        {
            return GetValueOrNil().AsBoolean();
        }

        /// <summary>
        /// Gets the Lua value as a table wrapper.
        /// </summary>
        public LuaTable AsTable()
        {
            return GetValueOrNil().AsTable();
        }

        /// <summary>
        /// Gets the Lua value as a function wrapper.
        /// </summary>
        public LuaFunction AsFunction()
        {
            return GetValueOrNil().AsFunction();
        }

        /// <summary>
        /// Gets the Lua value as a coroutine wrapper.
        /// </summary>
        public LuaCoroutine AsCoroutine()
        {
            return GetValueOrNil().AsCoroutine();
        }

        /// <summary>
        /// Gets the Lua tuple values.
        /// </summary>
        public LuaValue[] AsTuple()
        {
            DynValue[] tuple = GetValueOrNil().GetTupleValuesForFacade();
            LuaValue[] values = new LuaValue[tuple.Length];
            for (int i = 0; i < tuple.Length; i++)
            {
                values[i] = new LuaValue(tuple[i].GetOwnerScript(), tuple[i]);
            }

            return values;
        }

        /// <summary>
        /// Reads the value as a CLR type through the existing converter pipeline.
        /// </summary>
        public T Read<T>()
        {
            return GetValueOrNil().Read<T>();
        }

        /// <summary>
        /// Attempts to read the value as a CLR type through the existing converter pipeline.
        /// </summary>
        public bool TryRead<T>(out T value)
        {
            return GetValueOrNil().TryRead(out value);
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
            return new LuaValue(null, DynValue.FromString(value));
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
        internal Script OwnerScript => GetValueOrNil().GetOwnerScript();

        /// <summary>
        /// Wraps a native value after validating its intrinsic ownership against the producing
        /// script. Scalar and otherwise shared values remain ownerless.
        /// </summary>
        internal static LuaValue Wrap(Script script, DynValue value)
        {
            return new LuaValue(script, value);
        }

        /// <summary>
        /// Scalarizes and wraps a native result, normalizing no-value returns to nil.
        /// </summary>
        internal static LuaValue WrapResult(Script script, DynValue value)
        {
            DynValue scalar = value.ToScalar();
            return Wrap(script, scalar.Type == DataType.Void ? DynValue.Nil : scalar);
        }

        /// <summary>
        /// Returns the underlying VM value after validating engine ownership.
        /// </summary>
        internal DynValue ToDynValue(Script ownerScript)
        {
            DynValue value = GetValueOrNil();
            Script intrinsicOwner = value.GetOwnerScript();
            if (intrinsicOwner != null && !ReferenceEquals(intrinsicOwner, ownerScript))
            {
                throw new InvalidOperationException(
                    "Lua value belongs to a different LuaEngine instance."
                );
            }

            ownerScript?.ThrowIfDisposed();
            return value;
        }

        /// <summary>
        /// Returns the VM value after validating resource ownership. The caller already checked the
        /// target engine, so scalar literals avoid an extra disposed-engine branch on hot paths.
        /// </summary>
        internal DynValue ToDynValueAfterOwnerChecked(Script ownerScript)
        {
            DynValue value = GetValueOrNil();
            Script intrinsicOwner = value.GetOwnerScript();
            if (intrinsicOwner == null)
            {
                return value;
            }

            if (!ReferenceEquals(intrinsicOwner, ownerScript))
            {
                throw new InvalidOperationException(
                    "Lua value belongs to a different LuaEngine instance."
                );
            }

            intrinsicOwner.ThrowIfDisposed();
            return value;
        }

        private DynValue GetValueOrNil()
        {
            return _value;
        }
    }
}
