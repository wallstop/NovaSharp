namespace NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Errors;
    using Execution;

    /// <summary>
    /// A class representing a value in a Lua/NovaSharp script.
    /// </summary>
    public sealed class DynValue
    {
        private static int RefIdCounter;

        private readonly int _refId = ++RefIdCounter;
        private int _hashCode = -1;

        private bool _readOnly;
        private double _number;
        private object _object;
        private DataType _type;

        /// <summary>
        /// Gets a unique reference identifier. This is guaranteed to be unique only for dynvalues created in a single thread as it's not thread-safe.
        /// </summary>
        public int ReferenceId
        {
            get { return _refId; }
        }

        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        public DataType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets the function (valid only if the <see cref="Type"/> is <see cref="DataType.Function"/>)
        /// </summary>
        public Closure Function
        {
            get { return _object as Closure; }
        }

        /// <summary>
        /// Gets the numeric value (valid only if the <see cref="Type"/> is <see cref="DataType.Number"/>)
        /// </summary>
        public double Number
        {
            get { return _number; }
        }

        /// <summary>
        /// Gets the values in the tuple (valid only if the <see cref="Type"/> is Tuple).
        /// This field is currently also used to hold arguments in values whose <see cref="Type"/> is <see cref="DataType.TailCallRequest"/>.
        /// </summary>
        [SuppressMessage(
            "Performance",
            "CA1819:Properties should not return arrays",
            Justification = "Tuple semantics rely on sharing the backing array to avoid per-call allocations."
        )]
        public DynValue[] Tuple
        {
            get { return _object as DynValue[]; }
        }

        /// <summary>
        /// Gets the coroutine handle. (valid only if the <see cref="Type"/> is Thread).
        /// </summary>
        public Coroutine Coroutine
        {
            get { return _object as Coroutine; }
        }

        /// <summary>
        /// Gets the table (valid only if the <see cref="Type"/> is <see cref="DataType.Table"/>)
        /// </summary>
        public Table Table
        {
            get { return _object as Table; }
        }

        /// <summary>
        /// Gets the boolean value (valid only if the <see cref="Type"/> is <see cref="DataType.Boolean"/>)
        /// </summary>
        public bool Boolean
        {
            get { return Number != 0; }
        }

        /// <summary>
        /// Gets the string value (valid only if the <see cref="Type"/> is <see cref="DataType.String"/>)
        /// </summary>
        public string String
        {
            get { return _object as string; }
        }

        /// <summary>
        /// Gets the CLR callback (valid only if the <see cref="Type"/> is <see cref="DataType.ClrFunction"/>)
        /// </summary>
        public CallbackFunction Callback
        {
            get { return _object as CallbackFunction; }
        }

        /// <summary>
        /// Gets the tail call data.
        /// </summary>
        public TailCallData TailCallData
        {
            get { return _object as TailCallData; }
        }

        /// <summary>
        /// Gets the yield request data.
        /// </summary>
        public YieldRequest YieldRequest
        {
            get { return _object as YieldRequest; }
        }

        /// <summary>
        /// Gets the tail call data.
        /// </summary>
        public UserData UserData
        {
            get { return _object as UserData; }
        }

        /// <summary>
        /// Returns true if this instance is write protected.
        /// </summary>
        public bool ReadOnly
        {
            get { return _readOnly; }
        }

        /// <summary>
        /// Creates a new writable value initialized to Nil.
        /// </summary>
        public static DynValue NewNil()
        {
            return new DynValue();
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified boolean.
        /// </summary>
        public static DynValue NewBoolean(bool v)
        {
            return new DynValue() { _number = v ? 1 : 0, _type = DataType.Boolean };
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified number.
        /// </summary>
        public static DynValue NewNumber(double num)
        {
            return new DynValue()
            {
                _number = num,
                _type = DataType.Number,
                _hashCode = -1,
            };
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified string.
        /// </summary>
        public static DynValue NewString(string str)
        {
            return new DynValue() { _object = str, _type = DataType.String };
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified StringBuilder.
        /// </summary>
        public static DynValue NewString(StringBuilder sb)
        {
            if (sb == null)
            {
                throw new ArgumentNullException(nameof(sb));
            }

            return new DynValue() { _object = sb.ToString(), _type = DataType.String };
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified string using String.Format like syntax
        /// </summary>
        public static DynValue NewString(string format, params object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            object[] formatArgs = args ?? Array.Empty<object>();

            string formattedValue =
                formatArgs.Length == 0
                    ? format
                    : string.Format(CultureInfo.InvariantCulture, format, formatArgs);

            return new DynValue() { _object = formattedValue, _type = DataType.String };
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified coroutine.
        /// Internal use only, for external use, see Script.CoroutineCreate
        /// </summary>
        /// <param name="coroutine">The coroutine object.</param>
        /// <returns></returns>
        public static DynValue NewCoroutine(Coroutine coroutine)
        {
            return new DynValue() { _object = coroutine, _type = DataType.Thread };
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified closure (function).
        /// </summary>
        public static DynValue NewClosure(Closure function)
        {
            return new DynValue() { _object = function, _type = DataType.Function };
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified CLR callback.
        /// </summary>
        public static DynValue NewCallback(
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callBack,
            string name = null
        )
        {
            return new DynValue()
            {
                _object = new CallbackFunction(callBack, name),
                _type = DataType.ClrFunction,
            };
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified CLR callback.
        /// See also CallbackFunction.FromDelegate and CallbackFunction.FromMethodInfo factory methods.
        /// </summary>
        public static DynValue NewCallback(CallbackFunction function)
        {
            return new DynValue() { _object = function, _type = DataType.ClrFunction };
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified table.
        /// </summary>
        public static DynValue NewTable(Table table)
        {
            return new DynValue() { _object = table, _type = DataType.Table };
        }

        /// <summary>
        /// Creates a new writable value initialized to an empty prime table (a
        /// prime table is a table made only of numbers, strings, booleans and other
        /// prime tables).
        /// </summary>
        public static DynValue NewPrimeTable()
        {
            return NewTable(new Table(null));
        }

        /// <summary>
        /// Creates a new writable value initialized to an empty table.
        /// </summary>
        public static DynValue NewTable(Script script)
        {
            return NewTable(new Table(script));
        }

        /// <summary>
        /// Creates a new writable value initialized to with array contents.
        /// </summary>
        public static DynValue NewTable(Script script, params DynValue[] arrayValues)
        {
            return NewTable(new Table(script, arrayValues));
        }

        /// <summary>
        /// Creates a new request for a tail call. This is the preferred way to execute Lua/NovaSharp code from a callback,
        /// although it's not always possible to use it. When a function (callback or script closure) returns a
        /// TailCallRequest, the bytecode processor immediately executes the function contained in the request.
        /// By executing script in this way, a callback function ensures it's not on the stack anymore and thus a number
        /// of functionality (state savings, coroutines, etc) keeps working at full power.
        /// </summary>
        /// <param name="tailFn">The function to be called.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static DynValue NewTailCallReq(DynValue tailFn, params DynValue[] args)
        {
            return new DynValue()
            {
                _object = new TailCallData() { Args = args, Function = tailFn },
                _type = DataType.TailCallRequest,
            };
        }

        /// <summary>
        /// Creates a new request for a tail call. This is the preferred way to execute Lua/NovaSharp code from a callback,
        /// although it's not always possible to use it. When a function (callback or script closure) returns a
        /// TailCallRequest, the bytecode processor immediately executes the function contained in the request.
        /// By executing script in this way, a callback function ensures it's not on the stack anymore and thus a number
        /// of functionality (state savings, coroutines, etc) keeps working at full power.
        /// </summary>
        /// <param name="tailCallData">The data for the tail call.</param>
        /// <returns></returns>
        public static DynValue NewTailCallReq(TailCallData tailCallData)
        {
            return new DynValue() { _object = tailCallData, _type = DataType.TailCallRequest };
        }

        /// <summary>
        /// Creates a new request for a yield of the current coroutine.
        /// </summary>
        /// <param name="args">The yield arguments.</param>
        /// <returns></returns>
        public static DynValue NewYieldReq(DynValue[] args)
        {
            return new DynValue()
            {
                _object = new YieldRequest() { ReturnValues = args },
                _type = DataType.YieldRequest,
            };
        }

        /// <summary>
        /// Creates a new request for a yield of the current coroutine.
        /// </summary>
        /// <param name="args">The yield arguments.</param>
        /// <returns></returns>
        internal static DynValue NewForcedYieldReq()
        {
            return new DynValue()
            {
                _object = new YieldRequest() { Forced = true },
                _type = DataType.YieldRequest,
            };
        }

        /// <summary>
        /// Creates a new tuple initialized to the specified values.
        /// </summary>
        public static DynValue NewTuple(params DynValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Length == 0)
            {
                return NewNil();
            }

            if (values.Length == 1)
            {
                return values[0];
            }

            return new DynValue() { _object = values, _type = DataType.Tuple };
        }

        /// <summary>
        /// Creates a new tuple initialized to the specified values - which can be potentially other tuples
        /// </summary>
        public static DynValue NewTupleNested(params DynValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (!Array.Exists(values, v => v.Type == DataType.Tuple))
            {
                return NewTuple(values);
            }

            if (values.Length == 1)
            {
                return values[0];
            }

            List<DynValue> vals = new();

            foreach (DynValue v in values)
            {
                if (v.Type == DataType.Tuple)
                {
                    vals.AddRange(v.Tuple);
                }
                else
                {
                    vals.Add(v);
                }
            }

            return new DynValue() { _object = vals.ToArray(), _type = DataType.Tuple };
        }

        /// <summary>
        /// Creates a new userdata value
        /// </summary>
        public static DynValue NewUserData(UserData userData)
        {
            return new DynValue() { _object = userData, _type = DataType.UserData };
        }

        /// <summary>
        /// Returns this value as readonly - eventually cloning it in the process if it isn't readonly to start with.
        /// </summary>
        public DynValue AsReadOnly()
        {
            if (ReadOnly)
            {
                return this;
            }
            else
            {
                return Clone(true);
            }
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public DynValue Clone()
        {
            return Clone(ReadOnly);
        }

        /// <summary>
        /// Clones this instance, overriding the "readonly" status.
        /// </summary>
        /// <param name="readOnly">if set to <c>true</c> the new instance is set as readonly, or writeable otherwise.</param>
        /// <returns></returns>
        public DynValue Clone(bool readOnly)
        {
            DynValue v = new()
            {
                _object = _object,
                _number = _number,
                _hashCode = _hashCode,
                _type = _type,
                _readOnly = readOnly,
            };
            return v;
        }

        /// <summary>
        /// Clones this instance, returning a writable copy.
        /// </summary>
        /// <exception cref="System.ArgumentException">Can't clone Symbol values</exception>
        public DynValue CloneAsWritable()
        {
            return Clone(false);
        }

        /// <summary>
        /// A preinitialized, readonly instance, equaling Void
        /// </summary>
        public static DynValue Void { get; private set; }

        /// <summary>
        /// A preinitialized, readonly instance, equaling Nil
        /// </summary>
        public static DynValue Nil { get; private set; }

        /// <summary>
        /// A preinitialized, readonly instance, equaling True
        /// </summary>
        public static DynValue True { get; private set; }

        /// <summary>
        /// A preinitialized, readonly instance, equaling False
        /// </summary>
        public static DynValue False { get; private set; }

        static DynValue()
        {
            Nil = new DynValue() { _type = DataType.Nil }.AsReadOnly();
            Void = new DynValue() { _type = DataType.Void }.AsReadOnly();
            True = NewBoolean(true).AsReadOnly();
            False = NewBoolean(false).AsReadOnly();
        }

        /// <summary>
        /// Returns a string which is what it's expected to be output by the print function applied to this value.
        /// </summary>
        public string ToPrintString()
        {
            if (_object is RefIdObject refId)
            {
                string typeString = Type.ToLuaTypeString();

                if (_object is UserData ud)
                {
                    string str = ud.Descriptor.AsString(ud.Object);
                    if (str != null)
                    {
                        return str;
                    }
                }

                return refId.FormatTypeString(typeString);
            }

            switch (Type)
            {
                case DataType.String:
                    return String;
                case DataType.Tuple:
                    return string.Join("\t", Tuple.Select(t => t.ToPrintString()));
                case DataType.TailCallRequest:
                    return "(TailCallRequest -- INTERNAL!)";
                case DataType.YieldRequest:
                    return "(YieldRequest -- INTERNAL!)";
                default:
                    return ToString();
            }
        }

        /// <summary>
        /// Returns a string which is what it's expected to be output by debuggers.
        /// </summary>
        public string ToDebugPrintString()
        {
            if (_object is RefIdObject refid)
            {
                string typeString = Type.ToLuaTypeString();

                if (_object is UserData ud)
                {
                    string str = ud.Descriptor.AsString(ud.Object);
                    if (str != null)
                    {
                        return str;
                    }
                }

                return refid.FormatTypeString(typeString);
            }

            switch (Type)
            {
                case DataType.Tuple:
                    return string.Join("\t", Tuple.Select(t => t.ToPrintString()));
                case DataType.TailCallRequest:
                    return "(TailCallRequest)";
                case DataType.YieldRequest:
                    return "(YieldRequest)";
                default:
                    return ToString();
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            switch (Type)
            {
                case DataType.Void:
                    return "void";
                case DataType.Nil:
                    return "nil";
                case DataType.Boolean:
                    return Boolean ? "true" : "false";
                case DataType.Number:
                    return Number.ToString(CultureInfo.InvariantCulture);
                case DataType.String:
                    return "\"" + String + "\"";
                case DataType.Function:
                    return $"(Function {Function.EntryPointByteCodeLocation:X8})";
                case DataType.ClrFunction:
                    return "(Function CLR)";
                case DataType.Table:
                    return "(Table)";
                case DataType.Tuple:
                    return string.Join(", ", Tuple.Select(t => t.ToString()));
                case DataType.TailCallRequest:
                    return "Tail:(" + string.Join(", ", Tuple.Select(t => t.ToString())) + ")";
                case DataType.UserData:
                    return "(UserData)";
                case DataType.Thread:
                    return $"(Coroutine {Coroutine.ReferenceId:X8})";
                default:
                    return "(???)";
            }
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            if (_hashCode != -1)
            {
                return _hashCode;
            }

            int baseValue = ((int)(Type)) << 27;

            switch (Type)
            {
                case DataType.Void:
                case DataType.Nil:
                    _hashCode = 0;
                    break;
                case DataType.Boolean:
                    _hashCode = Boolean ? 1 : 2;
                    break;
                case DataType.Number:
                    _hashCode = baseValue ^ Number.GetHashCode();
                    break;
                case DataType.String:
                    _hashCode = baseValue ^ StringComparer.Ordinal.GetHashCode(String);
                    break;
                case DataType.Function:
                    _hashCode = baseValue ^ Function.GetHashCode();
                    break;
                case DataType.ClrFunction:
                    _hashCode = baseValue ^ Callback.GetHashCode();
                    break;
                case DataType.Table:
                    _hashCode = baseValue ^ Table.GetHashCode();
                    break;
                case DataType.Tuple:
                case DataType.TailCallRequest:
                    _hashCode = baseValue ^ Tuple.GetHashCode();
                    break;
                case DataType.UserData:
                case DataType.Thread:
                default:
                    _hashCode = 999;
                    break;
            }

            return _hashCode;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is not DynValue other)
            {
                return false;
            }

            if (
                (other.Type == DataType.Nil && Type == DataType.Void)
                || (other.Type == DataType.Void && Type == DataType.Nil)
            )
            {
                return true;
            }

            if (other.Type != Type)
            {
                return false;
            }

            switch (Type)
            {
                case DataType.Void:
                case DataType.Nil:
                    return true;
                case DataType.Boolean:
                    return Boolean == other.Boolean;
                case DataType.Number:
                    return Number == other.Number;
                case DataType.String:
                    return String == other.String;
                case DataType.Function:
                    return Function == other.Function;
                case DataType.ClrFunction:
                    return Callback == other.Callback;
                case DataType.Table:
                    return Table == other.Table;
                case DataType.Tuple:
                case DataType.TailCallRequest:
                    return Tuple == other.Tuple;
                case DataType.Thread:
                    return Coroutine == other.Coroutine;
                case DataType.UserData:
                {
                    UserData ud1 = UserData;
                    UserData ud2 = other.UserData;

                    if (ud1 == null || ud2 == null)
                    {
                        return false;
                    }

                    if (ud1.Descriptor != ud2.Descriptor)
                    {
                        return false;
                    }

                    if (ud1.Object == null && ud2.Object == null)
                    {
                        return true;
                    }

                    if (ud1.Object != null && ud2.Object != null)
                    {
                        return ud1.Object.Equals(ud2.Object);
                    }

                    return false;
                }
                default:
                    return ReferenceEquals(this, other);
            }
        }

        /// <summary>
        /// Casts this DynValue to string, using coercion if the type is number.
        /// </summary>
        /// <returns>The string representation, or null if not number, not string.</returns>
        public string CastToString()
        {
            DynValue rv = ToScalar();
            if (rv.Type == DataType.Number)
            {
                return rv.Number.ToString(CultureInfo.InvariantCulture);
            }
            else if (rv.Type == DataType.String)
            {
                return rv.String;
            }
            return null;
        }

        /// <summary>
        /// Casts this DynValue to a double, using coercion if the type is string.
        /// </summary>
        /// <returns>The string representation, or null if not number, not string or non-convertible-string.</returns>
        public double? CastToNumber()
        {
            DynValue rv = ToScalar();
            if (rv.Type == DataType.Number)
            {
                return rv.Number;
            }
            else if (rv.Type == DataType.String)
            {
                if (
                    double.TryParse(
                        rv.String,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double num
                    )
                )
                {
                    return num;
                }
            }
            return null;
        }

        /// <summary>
        /// Casts this DynValue to a bool
        /// </summary>
        /// <returns>False if value is false or nil, true otherwise.</returns>
        public bool CastToBool()
        {
            DynValue rv = ToScalar();
            if (rv.Type == DataType.Boolean)
            {
                return rv.Boolean;
            }
            else
            {
                return (rv.Type != DataType.Nil && rv.Type != DataType.Void);
            }
        }

        /// <summary>
        /// Returns this DynValue as an instance of <see cref="IScriptPrivateResource"/>, if possible,
        /// null otherwise.
        /// </summary>
        public IScriptPrivateResource ScriptPrivateResource
        {
            get { return _object as IScriptPrivateResource; }
        }

        /// <summary>
        /// Converts a tuple to a scalar value. If it's already a scalar value, this function returns "this".
        /// </summary>
        public DynValue ToScalar()
        {
            if (Type != DataType.Tuple)
            {
                return this;
            }

            if (Tuple.Length == 0)
            {
                return Void;
            }

            return Tuple[0].ToScalar();
        }

        /// <summary>
        /// Performs an assignment, overwriting the value with the specified one.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ScriptRuntimeException">If the value is readonly.</exception>
        public void Assign(DynValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (ReadOnly)
            {
                throw new ScriptRuntimeException("Assigning on r-value");
            }

            _number = value._number;
            _object = value._object;
            _type = value.Type;
            _hashCode = -1;
        }

        /// <summary>
        /// Gets the length of a string or table value.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ScriptRuntimeException">Value is not a table or string.</exception>
        public DynValue GetLength()
        {
            if (Type == DataType.Table)
            {
                return NewNumber(Table.Length);
            }

            if (Type == DataType.String)
            {
                return NewNumber(String.Length);
            }

            throw new ScriptRuntimeException("Can't get length of type {0}", Type);
        }

        /// <summary>
        /// Determines whether this instance is nil or void
        /// </summary>
        public bool IsNil()
        {
            return Type == DataType.Nil || Type == DataType.Void;
        }

        /// <summary>
        /// Determines whether this instance is not nil or void
        /// </summary>
        public bool IsNotNil()
        {
            return Type != DataType.Nil && Type != DataType.Void;
        }

        /// <summary>
        /// Determines whether this instance is void
        /// </summary>
        public bool IsVoid()
        {
            return Type == DataType.Void;
        }

        /// <summary>
        /// Determines whether this instance is not void
        /// </summary>
        public bool IsNotVoid()
        {
            return Type != DataType.Void;
        }

        /// <summary>
        /// Determines whether is nil, void or NaN (and thus unsuitable for using as a table key).
        /// </summary>
        public bool IsNilOrNan()
        {
            return (Type == DataType.Nil)
                || (Type == DataType.Void)
                || (Type == DataType.Number && double.IsNaN(Number));
        }

        /// <summary>
        /// Changes the numeric value of a number DynValue.
        /// </summary>
        internal void AssignNumber(double num)
        {
            if (ReadOnly)
            {
                throw new InternalErrorException(null, "Writing on r-value");
            }

            if (Type != DataType.Number)
            {
                throw new InternalErrorException("Can't assign number to type {0}", Type);
            }

            _number = num;
        }

        /// <summary>
        /// Creates a new DynValue from a CLR object
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static DynValue FromObject(Script script, object obj)
        {
            return Interop.Converters.ClrToScriptConversions.ObjectToDynValue(script, obj);
        }

        /// <summary>
        /// Converts this NovaSharp DynValue to a CLR object.
        /// </summary>
        public object ToObject()
        {
            return Interop.Converters.ScriptToClrConversions.DynValueToObject(this);
        }

        /// <summary>
        /// Converts this NovaSharp DynValue to a CLR object of the specified type.
        /// </summary>
        public object ToObject(Type desiredType)
        {
            if (desiredType == null)
            {
                throw new ArgumentNullException(nameof(desiredType));
            }

            //Contract.Requires(desiredType != null);
            return Interop.Converters.ScriptToClrConversions.DynValueToObjectOfType(
                this,
                desiredType,
                null,
                false
            );
        }

        /// <summary>
        /// Converts this NovaSharp DynValue to a CLR object of the specified type.
        /// </summary>
        public T ToObject<T>()
        {
            T myObject = (T)ToObject(typeof(T));
            if (myObject == null)
            {
                return default(T);
            }

            return myObject;
        }

#if HASDYNAMIC
        /// <summary>
        /// Converts this NovaSharp DynValue to a CLR object, marked as dynamic
        /// </summary>
        public dynamic ToDynamic()
        {
            return NovaSharp.Interpreter.Interop.Converters.ScriptToClrConversions.DynValueToObject(
                this
            );
        }
#endif

        /// <summary>
        /// Checks the type of this value corresponds to the desired type. A property ScriptRuntimeException is thrown
        /// if the value is not of the specified type or - considering the TypeValidationOptions - is not convertible
        /// to the specified type.
        /// </summary>
        /// <param name="funcName">Name of the function requesting the value, for error message purposes.</param>
        /// <param name="desiredType">The desired data type.</param>
        /// <param name="argNum">The argument number, for error message purposes.</param>
        /// <param name="flags">The TypeValidationOptions.</param>
        /// <returns></returns>
        /// <exception cref="ScriptRuntimeException">Thrown
        /// if the value is not of the specified type or - considering the TypeValidationOptions - is not convertible
        /// to the specified type.</exception>
        public DynValue CheckType(
            string funcName,
            DataType desiredType,
            int argNum = -1,
            TypeValidationOptions flags = TypeValidationOptions.None
        )
        {
            if (Type == desiredType)
            {
                return this;
            }

            bool allowNil = ((int)(flags & TypeValidationOptions.AllowNil) != 0);

            if (allowNil && IsNil())
            {
                return this;
            }

            bool autoConvert = ((int)(flags & TypeValidationOptions.AutoConvert) != 0);

            if (autoConvert)
            {
                if (desiredType == DataType.Boolean)
                {
                    return NewBoolean(CastToBool());
                }

                if (desiredType == DataType.Number)
                {
                    double? v = CastToNumber();
                    if (v.HasValue)
                    {
                        return NewNumber(v.Value);
                    }
                }

                if (desiredType == DataType.String)
                {
                    string v = CastToString();
                    if (v != null)
                    {
                        return NewString(v);
                    }
                }
            }

            if (IsVoid())
            {
                throw ScriptRuntimeException.BadArgumentNoValue(argNum, funcName, desiredType);
            }

            throw ScriptRuntimeException.BadArgument(argNum, funcName, desiredType, Type, allowNil);
        }

        /// <summary>
        /// Checks if the type is a specific userdata type, and returns it or throws.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="funcName">Name of the function.</param>
        /// <param name="argNum">The argument number.</param>
        /// <param name="flags">The flags.</param>
        /// <returns></returns>
        public T CheckUserDataType<T>(
            string funcName,
            int argNum = -1,
            TypeValidationOptions flags = TypeValidationOptions.None
        )
        {
            DynValue v = CheckType(funcName, DataType.UserData, argNum, flags);
            bool allowNil = ((int)(flags & TypeValidationOptions.AllowNil) != 0);

            if (v.IsNil())
            {
                return default(T);
            }

            object o = v.UserData.Object;
            if (o is T o1)
            {
                return o1;
            }

            throw ScriptRuntimeException.BadArgumentUserData(
                argNum,
                funcName,
                typeof(T),
                o,
                allowNil
            );
        }
    }
}
