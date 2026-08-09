namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text;
    using global::NovaSharp;
    using Compatibility;
    using Cysharp.Text;
    using DataStructs;
    using Errors;
    using Execution;

    /// <summary>
    /// A value in a Lua/NovaSharp script.
    /// </summary>
    /// <remarks>
    /// Values are immutable once constructed. Mutable storage lives in the VM's local/upvalue
    /// slots instead, so a value can be shared freely (pushed on the value stack, used as a table
    /// key, embedded in an instruction literal) without defensive copying.
    /// </remarks>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct DynValue : IEquatable<DynValue>
    {
        private readonly LuaNumber _number;
        private readonly object _object;
        private readonly DataType _type;

        private DynValue(DataType type)
        {
            _type = type;
            _number = default;
            _object = null;
        }

        private DynValue(DataType type, LuaNumber number)
        {
            _type = type;
            _number = number;
            _object = null;
        }

        private DynValue(DataType type, object reference)
        {
            _type = type;
            _number = default;
            _object = reference;
        }

        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        public DataType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets the public Lua value kind.
        /// </summary>
        public LuaKind Kind
        {
            get { return ToFacadeKind(this); }
        }

        /// <summary>
        /// Gets whether this value is a number.
        /// </summary>
        public bool IsNumber => _type == DataType.Number;

        /// <summary>
        /// Gets whether this value is a string.
        /// </summary>
        public bool IsString => _type == DataType.String;

        /// <summary>
        /// Gets whether this value is a table.
        /// </summary>
        public bool IsTable => _type == DataType.Table;

        /// <summary>
        /// Gets whether this value is callable directly.
        /// </summary>
        public bool IsFunction => _type == DataType.Function || _type == DataType.ClrFunction;

        /// <summary>
        /// Gets the reference payload with no type test, or <c>null</c> for values that carry none.
        /// </summary>
        /// <remarks>
        /// Lets hot lookup paths (table hash probes) compare a candidate key by reference without
        /// paying for the <c>isinst</c> that the typed accessors such as <see cref="String"/> emit.
        /// Callers must establish the type themselves before treating the result as anything
        /// specific.
        /// </remarks>
        internal object ReferencePayload
        {
            get { return _object; }
        }

        /// <summary>
        /// Determines whether two reference-backed values carry the same Lua identity.
        /// </summary>
        /// <remarks>
        /// This is deliberately narrower than <see cref="Equals(DynValue)"/>. In particular,
        /// userdata equality can use descriptor/CLR-object semantics after the VM has given an
        /// identical userdata payload the raw-identity fast path required by Lua.
        /// </remarks>
        internal bool HasSameReferenceIdentity(DynValue other)
        {
            if (_type != other._type)
            {
                return false;
            }

            switch (_type)
            {
                case DataType.String:
                case DataType.Function:
                case DataType.Table:
                case DataType.Tuple:
                case DataType.UserData:
                case DataType.Thread:
                case DataType.ClrFunction:
                case DataType.TailCallRequest:
                case DataType.YieldRequest:
                    return ReferenceEquals(_object, other._object);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the function (valid only if the <see cref="Type"/> is <see cref="DataType.Function"/>)
        /// </summary>
        public Closure Function
        {
            get { return _object as Closure; }
        }

        /// <summary>
        /// Gets the numeric value as a double (valid only if the <see cref="Type"/> is <see cref="DataType.Number"/>).
        /// For Lua 5.3+ integer/float distinction, use <see cref="LuaNumber"/>, <see cref="IsInteger"/>, and <see cref="IsFloat"/>.
        /// </summary>
        public double Number
        {
            get { return _number.ToDouble; }
        }

        /// <summary>
        /// Gets the underlying <see cref="LuaNumber"/> value (valid only if the <see cref="Type"/> is <see cref="DataType.Number"/>).
        /// This provides access to the Lua 5.3+ integer/float subtype discrimination.
        /// </summary>
        public LuaNumber LuaNumber
        {
            get { return _number; }
        }

        /// <summary>
        /// Gets a value indicating whether this number is a Lua integer subtype.
        /// Valid only if <see cref="Type"/> is <see cref="DataType.Number"/>.
        /// </summary>
        public bool IsInteger
        {
            get { return _type == DataType.Number && _number.IsInteger; }
        }

        /// <summary>
        /// Gets a value indicating whether this number is a Lua float subtype.
        /// Valid only if <see cref="Type"/> is <see cref="DataType.Number"/>.
        /// </summary>
        public bool IsFloat
        {
            get { return _type == DataType.Number && _number.IsFloat; }
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
        [SuppressMessage(
            "Naming",
            "CA1720:Identifier contains type name",
            Justification = "DynValue exposes typed accessors that intentionally mirror Lua's DataType names."
        )]
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
        /// Gets the Lua number as a double.
        /// </summary>
        public double AsNumber()
        {
            if (_type != DataType.Number)
            {
                throw NewFacadeKindException(nameof(AsNumber), "Number", Kind);
            }

            return Number;
        }

        /// <summary>
        /// Gets the Lua number as a 64-bit integer.
        /// </summary>
        public long AsInteger()
        {
            if (_type != DataType.Number || !IsInteger)
            {
                throw NewFacadeKindException(nameof(AsInteger), LuaKind.Integer, Kind);
            }

            return _number.AsInteger;
        }

        /// <summary>
        /// Gets the Lua value as a string.
        /// </summary>
        public string AsString()
        {
            return RequireFacadeType(DataType.String, nameof(AsString)).String;
        }

        /// <summary>
        /// Gets the Lua value as a Boolean.
        /// </summary>
        public bool AsBoolean()
        {
            return RequireFacadeType(DataType.Boolean, nameof(AsBoolean)).Boolean;
        }

        /// <summary>
        /// Gets the Lua value as a table wrapper.
        /// </summary>
        public LuaTable AsTable()
        {
            DynValue value = RequireFacadeType(DataType.Table, nameof(AsTable));
            return new LuaTable(GetFacadeOwnerOrThrow(), value.Table);
        }

        /// <summary>
        /// Gets the Lua value as a function wrapper.
        /// </summary>
        public LuaFunction AsFunction()
        {
            if (!IsFunction)
            {
                throw NewFacadeKindException(nameof(AsFunction), LuaKind.Function, Kind);
            }

            return new LuaFunction(GetFacadeOwnerOrThrow(), this);
        }

        /// <summary>
        /// Gets the Lua value as a coroutine wrapper.
        /// </summary>
        public LuaCoroutine AsCoroutine()
        {
            DynValue value = RequireFacadeType(DataType.Thread, nameof(AsCoroutine));
            return new LuaCoroutine(GetFacadeOwnerOrThrow(), value);
        }

        /// <summary>
        /// Gets a copy of the Lua tuple values.
        /// </summary>
        public DynValue[] AsTuple()
        {
            DynValue[] tuple = GetTupleValuesForFacade();
            DynValue[] result = new DynValue[tuple.Length];
            Array.Copy(tuple, result, tuple.Length);
            return result;
        }

        /// <summary>
        /// Gets the tuple backing values after applying public facade validation.
        /// </summary>
        internal DynValue[] GetTupleValuesForFacade()
        {
            DynValue value = RequireFacadeType(DataType.Tuple, nameof(AsTuple));
            value.GetOwnerScript()?.ThrowIfDisposed();
            return value.Tuple;
        }

        /// <summary>
        /// Reads the value as a CLR type through the existing converter pipeline.
        /// </summary>
        public T Read<T>()
        {
            try
            {
                this.GetOwnerScript()?.ThrowIfDisposed();
                return ToObject<T>();
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
        /// Returns the nil value.
        /// </summary>
        public static DynValue NewNil()
        {
            return Nil;
        }

        /// <summary>
        /// Creates a value equal to the specified boolean.
        /// </summary>
        public static DynValue NewBoolean(bool v)
        {
            return new DynValue(DataType.Boolean, LuaNumber.FromInteger(v ? 1L : 0L));
        }

        /// <summary>
        /// Returns a boolean value.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        /// <returns>A <see cref="DynValue"/> representing the boolean.</returns>
        public static DynValue FromBoolean(bool value)
        {
            return value ? True : False;
        }

        /// <summary>
        /// Creates a value initialized to the specified number as a float subtype.
        /// </summary>
        public static DynValue NewNumber(double num)
        {
            return new DynValue(DataType.Number, LuaNumber.FromDouble(num));
        }

        /// <summary>
        /// Creates a value initialized to the specified number with explicit float subtype.
        /// Unlike <see cref="NewNumber(double)"/>, this method preserves the float subtype even for
        /// whole numbers like 3.0, which is required for Lua 5.3+ compliance with numeric literals.
        /// </summary>
        public static DynValue NewFloat(double num)
        {
            return new DynValue(DataType.Number, LuaNumber.FromFloat(num));
        }

        /// <summary>
        /// Creates a value initialized to the specified integer.
        /// The resulting value will have the Lua "integer" subtype.
        /// </summary>
        public static DynValue NewInteger(long num)
        {
            return new DynValue(DataType.Number, LuaNumber.FromInteger(num));
        }

        /// <summary>
        /// Creates a value initialized to the specified <see cref="LuaNumber"/>.
        /// </summary>
        public static DynValue NewNumber(LuaNumber num)
        {
            return new DynValue(DataType.Number, num);
        }

        /// <summary>
        /// Returns a number value.
        /// </summary>
        /// <param name="num">The number value.</param>
        /// <returns>A <see cref="DynValue"/> representing the number.</returns>
        public static DynValue FromNumber(double num)
        {
            return NewNumber(num);
        }

        /// <summary>
        /// Returns a float value while preserving its Lua float subtype.
        /// </summary>
        /// <param name="num">The float value.</param>
        /// <returns>A <see cref="DynValue"/> representing the float.</returns>
        public static DynValue FromFloat(double num)
        {
            return NewFloat(num);
        }

        /// <summary>
        /// Returns an integer value with the Lua integer subtype.
        /// </summary>
        /// <param name="num">The integer value.</param>
        /// <returns>A <see cref="DynValue"/> representing the integer.</returns>
        public static DynValue FromInteger(long num)
        {
            return NewInteger(num);
        }

        /// <summary>
        /// Creates a value initialized to the specified string.
        /// </summary>
        public static DynValue NewString(string str)
        {
            return new DynValue(DataType.String, str);
        }

        /// <summary>
        /// Returns a string value, or nil when the CLR string is null.
        /// </summary>
        public static DynValue FromString(string value)
        {
            return value == null ? Nil : NewString(value);
        }

        /// <summary>
        /// Creates a value initialized to the specified StringBuilder.
        /// </summary>
        public static DynValue NewString(StringBuilder sb)
        {
            if (sb == null)
            {
                throw new ArgumentNullException(nameof(sb));
            }

            return new DynValue(DataType.String, sb.ToString());
        }

        /// <summary>
        /// Creates a value initialized to the specified string using String.Format like syntax
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

            return new DynValue(DataType.String, formattedValue);
        }

        /// <summary>
        /// Creates a new string value by concatenating two strings using ZString for zero-allocation performance.
        /// This is an internal API optimized for the VM's CONCAT opcode and expression evaluation.
        /// </summary>
        /// <param name="left">The left string.</param>
        /// <param name="right">The right string.</param>
        /// <returns>A new <see cref="DynValue"/> containing the concatenated string.</returns>
        /// <remarks>
        /// Uses ZString.Concat internally to avoid intermediate string allocations
        /// when concatenating two strings. For concatenating more than two strings
        /// in a loop, consider using <see cref="NewStringFromBuilder"/> with a
        /// <see cref="Utf16ValueStringBuilder"/> instead.
        /// </remarks>
        internal static DynValue NewConcatenatedString(string left, string right)
        {
            return new DynValue(DataType.String, ZString.Concat(left, right));
        }

        /// <summary>
        /// Creates a new string value by concatenating three strings using ZString for zero-allocation performance.
        /// </summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <param name="s3">The third string.</param>
        /// <returns>A new <see cref="DynValue"/> containing the concatenated string.</returns>
        internal static DynValue NewConcatenatedString(string s1, string s2, string s3)
        {
            return new DynValue(DataType.String, ZString.Concat(s1, s2, s3));
        }

        /// <summary>
        /// Creates a new string value by concatenating four strings using ZString for zero-allocation performance.
        /// </summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <param name="s3">The third string.</param>
        /// <param name="s4">The fourth string.</param>
        /// <returns>A new <see cref="DynValue"/> containing the concatenated string.</returns>
        internal static DynValue NewConcatenatedString(string s1, string s2, string s3, string s4)
        {
            return new DynValue(DataType.String, ZString.Concat(s1, s2, s3, s4));
        }

        /// <summary>
        /// Creates a new string value from a <see cref="Utf16ValueStringBuilder"/>.
        /// This is an internal API for building strings efficiently in loops.
        /// </summary>
        /// <param name="builder">The ZString builder containing the accumulated string.</param>
        /// <returns>A new <see cref="DynValue"/> containing the built string.</returns>
        /// <remarks>
        /// The caller is responsible for disposing the builder after this call.
        /// Usage pattern:
        /// <code>
        /// using var sb = ZStringBuilder.Create();
        /// sb.Append("hello");
        /// sb.Append(" world");
        /// return DynValue.NewStringFromBuilder(sb);
        /// </code>
        /// </remarks>
        internal static DynValue NewStringFromBuilder(Utf16ValueStringBuilder builder)
        {
            return new DynValue(DataType.String, builder.ToString());
        }

        /// <summary>
        /// Creates a value initialized to the specified coroutine.
        /// Internal use only, for external use, see Script.CoroutineCreate
        /// </summary>
        /// <param name="coroutine">The coroutine object.</param>
        /// <returns></returns>
        public static DynValue NewCoroutine(Coroutine coroutine)
        {
            return new DynValue(DataType.Thread, coroutine);
        }

        /// <summary>
        /// Creates a value initialized to the specified closure (function).
        /// </summary>
        public static DynValue NewClosure(Closure function)
        {
            return new DynValue(DataType.Function, function);
        }

        /// <summary>
        /// Returns a DynValue wrapping the specified closure.
        /// </summary>
        /// <param name="closure">The closure to wrap.</param>
        /// <returns>A <see cref="DynValue"/> representing the closure.</returns>
        internal static DynValue FromClosure(Closure closure)
        {
            if (closure == null)
            {
                return Nil;
            }

            return NewClosure(closure);
        }

        /// <summary>
        /// Returns a DynValue wrapping the specified table.
        /// </summary>
        /// <param name="table">The table to wrap.</param>
        /// <returns>A <see cref="DynValue"/> representing the table.</returns>
        internal static DynValue FromTable(Table table)
        {
            if (table == null)
            {
                return Nil;
            }

            return NewTable(table);
        }

        /// <summary>
        /// Returns a DynValue wrapping the specified CLR callback.
        /// </summary>
        internal static DynValue FromCallback(CallbackFunction function)
        {
            if (function == null)
            {
                return Nil;
            }

            return NewCallback(function);
        }

        /// <summary>
        /// Binds a shared callback payload to a script while preserving non-callback values.
        /// </summary>
        internal DynValue BindCallbackToScript(Script script)
        {
            return Type == DataType.ClrFunction ? NewCallback(Callback.BindToScript(script)) : this;
        }

        /// <summary>
        /// Creates a value initialized to the specified CLR callback.
        /// </summary>
        public static DynValue NewCallback(
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callBack,
            string name = null
        )
        {
            return new DynValue(DataType.ClrFunction, new CallbackFunction(callBack, name));
        }

        /// <summary>
        /// Creates a callback owned by the specified script.
        /// </summary>
        internal static DynValue NewCallback(
            Script ownerScript,
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callBack,
            string name = null
        )
        {
            return new DynValue(
                DataType.ClrFunction,
                new CallbackFunction(ownerScript, callBack, name)
            );
        }

        /// <summary>
        /// Creates a value initialized to a CLR callback that receives a stack-only argument view.
        /// </summary>
        public static DynValue NewCallbackView(
            ScriptFunctionCallbackView callBack,
            string name = null
        )
        {
            return new DynValue(
                DataType.ClrFunction,
                CallbackFunction.FromArgumentView(callBack, name)
            );
        }

        /// <summary>
        /// Creates a script-owned callback that receives a stack-only argument view.
        /// </summary>
        internal static DynValue NewCallbackView(
            Script ownerScript,
            ScriptFunctionCallbackView callBack,
            string name = null
        )
        {
            return new DynValue(
                DataType.ClrFunction,
                CallbackFunction.FromArgumentView(ownerScript, callBack, name)
            );
        }

        /// <summary>
        /// Creates a value initialized to a CLR callback that receives a stack-only
        /// argument view and does not require a script execution context.
        /// </summary>
        public static DynValue NewCallbackView(
            ScriptFunctionCallbackViewNoContext callBack,
            string name = null
        )
        {
            return new DynValue(
                DataType.ClrFunction,
                CallbackFunction.FromArgumentView(callBack, name)
            );
        }

        /// <summary>
        /// Creates a script-owned callback that receives a stack-only argument view and does not
        /// require an execution context.
        /// </summary>
        internal static DynValue NewCallbackView(
            Script ownerScript,
            ScriptFunctionCallbackViewNoContext callBack,
            string name = null
        )
        {
            return new DynValue(
                DataType.ClrFunction,
                CallbackFunction.FromArgumentView(ownerScript, callBack, name)
            );
        }

        /// <summary>
        /// Creates a value initialized to the specified CLR callback.
        /// See also CallbackFunction.FromDelegate and CallbackFunction.FromMethodInfo factory methods.
        /// </summary>
        public static DynValue NewCallback(CallbackFunction function)
        {
            return new DynValue(DataType.ClrFunction, function);
        }

        /// <summary>
        /// Creates a value initialized to the specified table.
        /// </summary>
        public static DynValue NewTable(Table table)
        {
            return new DynValue(DataType.Table, table);
        }

        /// <summary>
        /// Creates a value initialized to an empty prime table (a
        /// prime table is a table made only of numbers, strings, booleans and other
        /// prime tables).
        /// </summary>
        public static DynValue NewPrimeTable()
        {
            return NewTable(new Table(null));
        }

        /// <summary>
        /// Creates a value initialized to an empty table.
        /// </summary>
        public static DynValue NewTable(Script script)
        {
            return NewTable(new Table(script));
        }

        /// <summary>
        /// Creates a value initialized with array contents.
        /// </summary>
        public static DynValue NewTable(Script script, params DynValue[] arrayValues)
        {
            return NewTable(new Table(script, arrayValues));
        }

        /// <summary>
        /// Creates a new request for a tail call with no arguments.
        /// </summary>
        /// <param name="tailFn">The function to be called.</param>
        /// <returns></returns>
        public static DynValue NewTailCallReq(DynValue tailFn)
        {
            return new DynValue(
                DataType.TailCallRequest,
                new TailCallData() { Args = Array.Empty<DynValue>(), Function = tailFn }
            );
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
            return new DynValue(
                DataType.TailCallRequest,
                new TailCallData() { Args = args, Function = tailFn }
            );
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
            return new DynValue(DataType.TailCallRequest, tailCallData);
        }

        /// <summary>
        /// Creates a new request for a yield of the current coroutine.
        /// </summary>
        /// <param name="args">The yield arguments.</param>
        /// <returns></returns>
        public static DynValue NewYieldReq(DynValue[] args)
        {
            return new DynValue(DataType.YieldRequest, new YieldRequest() { ReturnValues = args });
        }

        /// <summary>
        /// Creates a new request for a yield of the current coroutine with no return values.
        /// </summary>
        /// <returns>A yield request <see cref="DynValue"/>.</returns>
        internal static DynValue NewYieldReq()
        {
            return new DynValue(DataType.YieldRequest, new YieldRequest());
        }

        /// <summary>
        /// Creates a new request for a yield of the current coroutine with one return value.
        /// </summary>
        /// <param name="arg">The yielded return value.</param>
        /// <returns>A yield request <see cref="DynValue"/>.</returns>
        internal static DynValue NewYieldReq(DynValue arg)
        {
            return new DynValue(DataType.YieldRequest, YieldRequest.New(arg));
        }

        /// <summary>
        /// Creates a new request for a yield of the current coroutine with two return values.
        /// </summary>
        /// <param name="arg0">The first yielded return value.</param>
        /// <param name="arg1">The second yielded return value.</param>
        /// <returns>A yield request <see cref="DynValue"/>.</returns>
        internal static DynValue NewYieldReq(DynValue arg0, DynValue arg1)
        {
            return new DynValue(DataType.YieldRequest, YieldRequest.New(arg0, arg1));
        }

        /// <summary>
        /// Creates a new request for a yield of the current coroutine with three return values.
        /// </summary>
        /// <param name="arg0">The first yielded return value.</param>
        /// <param name="arg1">The second yielded return value.</param>
        /// <param name="arg2">The third yielded return value.</param>
        /// <returns>A yield request <see cref="DynValue"/>.</returns>
        internal static DynValue NewYieldReq(DynValue arg0, DynValue arg1, DynValue arg2)
        {
            return new DynValue(DataType.YieldRequest, YieldRequest.New(arg0, arg1, arg2));
        }

        /// <summary>
        /// Creates a new request for a yield of the current coroutine with four return values.
        /// </summary>
        /// <param name="arg0">The first yielded return value.</param>
        /// <param name="arg1">The second yielded return value.</param>
        /// <param name="arg2">The third yielded return value.</param>
        /// <param name="arg3">The fourth yielded return value.</param>
        /// <returns>A yield request <see cref="DynValue"/>.</returns>
        internal static DynValue NewYieldReq(
            DynValue arg0,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3
        )
        {
            return new DynValue(DataType.YieldRequest, YieldRequest.New(arg0, arg1, arg2, arg3));
        }

        /// <summary>
        /// Creates a new request for a yield of the current coroutine.
        /// </summary>
        /// <param name="args">The yield arguments.</param>
        /// <returns></returns>
        internal static DynValue NewForcedYieldReq()
        {
            return new DynValue(DataType.YieldRequest, new YieldRequest() { Forced = true });
        }

        /// <summary>
        /// Creates a new tuple initialized to a single value.
        /// This is an optimized overload that returns the value directly (no array allocation).
        /// </summary>
        public static DynValue NewTuple(DynValue value)
        {
            return value;
        }

        /// <summary>
        /// Creates a new tuple initialized to two values.
        /// This is an optimized overload that avoids params array allocation.
        /// </summary>
        public static DynValue NewTuple(DynValue value1, DynValue value2)
        {
            return new DynValue(DataType.Tuple, new[] { value1, value2 });
        }

        /// <summary>
        /// Creates a new tuple initialized to three values.
        /// This is an optimized overload that avoids params array allocation.
        /// </summary>
        public static DynValue NewTuple(DynValue value1, DynValue value2, DynValue value3)
        {
            return new DynValue(DataType.Tuple, new[] { value1, value2, value3 });
        }

        /// <summary>
        /// Creates a new tuple initialized to four values.
        /// This is an optimized overload that avoids params array allocation.
        /// </summary>
        public static DynValue NewTuple(
            DynValue value1,
            DynValue value2,
            DynValue value3,
            DynValue value4
        )
        {
            return new DynValue(DataType.Tuple, new[] { value1, value2, value3, value4 });
        }

        /// <summary>
        /// Creates a new tuple initialized to five values.
        /// This is an optimized overload that avoids params array allocation.
        /// </summary>
        public static DynValue NewTuple(
            DynValue value1,
            DynValue value2,
            DynValue value3,
            DynValue value4,
            DynValue value5
        )
        {
            return new DynValue(DataType.Tuple, new[] { value1, value2, value3, value4, value5 });
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
                return EmptyTuple;
            }

            if (values.Length == 1)
            {
                return values[0];
            }

            return new DynValue(DataType.Tuple, values);
        }

        /// <summary>
        /// Creates a new tuple initialized to a single value - which can be potentially a tuple.
        /// Returns the value directly (tuple flattening).
        /// </summary>
        public static DynValue NewTupleNested(DynValue value)
        {
            return value;
        }

        /// <summary>
        /// Creates a new tuple initialized to two values - which can be potentially other tuples.
        /// This is an optimized overload that avoids params array allocation.
        /// </summary>
        public static DynValue NewTupleNested(DynValue value1, DynValue value2)
        {
            // Fast path: neither is a tuple
            if (value1.Type != DataType.Tuple && value2.Type != DataType.Tuple)
            {
                return NewTuple(value1, value2);
            }

            // Slow path: flatten tuples
            int capacity =
                (value1.Type == DataType.Tuple ? value1.Tuple.Length : 1)
                + (value2.Type == DataType.Tuple ? value2.Tuple.Length : 1);
            using (ListPool<DynValue>.Get(capacity, out List<DynValue> vals))
            {
                if (value1.Type == DataType.Tuple)
                {
                    vals.AddRange(value1.Tuple);
                }
                else
                {
                    vals.Add(value1);
                }

                if (value2.Type == DataType.Tuple)
                {
                    vals.AddRange(value2.Tuple);
                }
                else
                {
                    vals.Add(value2);
                }

                return new DynValue(DataType.Tuple, ListPool<DynValue>.ToExactArray(vals));
            }
        }

        /// <summary>
        /// Creates a new tuple initialized to three values - which can be potentially other tuples.
        /// This is an optimized overload that avoids params array allocation.
        /// </summary>
        public static DynValue NewTupleNested(DynValue value1, DynValue value2, DynValue value3)
        {
            // Fast path: none are tuples
            if (
                value1.Type != DataType.Tuple
                && value2.Type != DataType.Tuple
                && value3.Type != DataType.Tuple
            )
            {
                return NewTuple(value1, value2, value3);
            }

            // Slow path: flatten tuples
            int capacity =
                (value1.Type == DataType.Tuple ? value1.Tuple.Length : 1)
                + (value2.Type == DataType.Tuple ? value2.Tuple.Length : 1)
                + (value3.Type == DataType.Tuple ? value3.Tuple.Length : 1);
            using (ListPool<DynValue>.Get(capacity, out List<DynValue> vals))
            {
                if (value1.Type == DataType.Tuple)
                {
                    vals.AddRange(value1.Tuple);
                }
                else
                {
                    vals.Add(value1);
                }

                if (value2.Type == DataType.Tuple)
                {
                    vals.AddRange(value2.Tuple);
                }
                else
                {
                    vals.Add(value2);
                }

                if (value3.Type == DataType.Tuple)
                {
                    vals.AddRange(value3.Tuple);
                }
                else
                {
                    vals.Add(value3);
                }

                return new DynValue(DataType.Tuple, ListPool<DynValue>.ToExactArray(vals));
            }
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

            if (values.Length == 0)
            {
                return Nil;
            }

            if (values.Length == 1)
            {
                return values[0];
            }

            if (!Array.Exists(values, v => v.Type == DataType.Tuple))
            {
                return NewTuple(values);
            }

            // Calculate capacity for the flattened list
            int capacity = 0;
            foreach (DynValue v in values)
            {
                capacity += v.Type == DataType.Tuple ? v.Tuple.Length : 1;
            }

            using (ListPool<DynValue>.Get(capacity, out List<DynValue> vals))
            {
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

                return new DynValue(DataType.Tuple, ListPool<DynValue>.ToExactArray(vals));
            }
        }

        /// <summary>
        /// Creates a new userdata value
        /// </summary>
        public static DynValue NewUserData(UserData userData)
        {
            return new DynValue(DataType.UserData, userData);
        }

        /// <summary>
        /// <summary>
        /// A preinitialized, readonly instance, equaling Void
        /// </summary>
        public static DynValue Void { get; }

        /// <summary>
        /// A preinitialized, readonly instance, equaling Nil
        /// </summary>
        public static DynValue Nil { get; }

        /// <summary>
        /// A preinitialized, readonly instance, equaling True
        /// </summary>
        public static DynValue True { get; }

        /// <summary>
        /// A preinitialized, readonly instance, equaling False
        /// </summary>
        public static DynValue False { get; }

        /// <summary>
        /// A preinitialized, readonly instance, equaling an empty string
        /// </summary>
        public static DynValue EmptyString { get; }

        /// <summary>
        /// A preinitialized, readonly instance representing an empty tuple (0 elements).
        /// This is semantically different from Nil: an empty tuple means "no values"
        /// while Nil means "the value nil". This distinction is important for varargs
        /// handling where select("#", ...) should return 0 for empty varargs, not 1.
        /// </summary>
        public static DynValue EmptyTuple { get; }

        static DynValue()
        {
            Nil = default;
            Void = new DynValue(DataType.Void);
            True = NewBoolean(true);
            False = NewBoolean(false);
            EmptyString = NewString(string.Empty);
            EmptyTuple = new DynValue(DataType.Tuple, Array.Empty<DynValue>());
        }

        /// <summary>
        /// Returns a string which is what it's expected to be output by the print function applied to this value.
        /// Uses the default Lua version for number formatting.
        /// </summary>
        public string ToPrintString()
        {
            return ToPrintString(LuaVersionDefaults.CurrentDefault);
        }

        /// <summary>
        /// Returns a string which is what it's expected to be output by the print function applied to this value,
        /// using the specified Lua version for number formatting.
        /// </summary>
        /// <param name="version">The Lua compatibility version to use for number formatting.</param>
        /// <returns>The print-friendly string representation of this value.</returns>
        /// <remarks>
        /// Number formatting differences by version:
        /// - Lua 5.1/5.2: Integer-like floats (e.g., 42.0) format as "42"
        /// - Lua 5.3+: Integer-like floats format as "42.0" to distinguish from integers
        /// </remarks>
        public string ToPrintString(LuaCompatibilityVersion version)
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
                case DataType.Number:
                    // Use LuaNumber.ToLuaString for version-aware formatting
                    return LuaNumber.ToLuaString(version);
                case DataType.Tuple:
                    return JoinTupleStrings(Tuple, "\t", v => v.ToPrintString(version));
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
                    return JoinTupleStrings(Tuple, "\t", v => v.ToPrintString());
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
                    return LuaKeywords.Nil;
                case DataType.Boolean:
                    return Boolean ? LuaKeywords.True : LuaKeywords.False;
                case DataType.Number:
                    // Use LuaNumber.ToString() to properly format infinity as "inf" and NaN as "nan"
                    return LuaNumber.ToString();
                case DataType.String:
                    // Use ZString.Concat for zero-allocation string building.
                    // JoinTupleStrings already uses notNested: false so recursive calls are safe.
                    return ZString.Concat("\"", String, "\"");
                case DataType.Function:
                    return ZString.Format(
                        "(Function 0x{0:x})",
                        Function.EntryPointByteCodeLocation
                    );
                case DataType.ClrFunction:
                    return "(Function CLR)";
                case DataType.Table:
                    return "(Table)";
                case DataType.Tuple:
                    return JoinTupleStrings(Tuple, ", ", v => v.ToString());
                case DataType.TailCallRequest:
                {
                    string tupleStr = JoinTupleStrings(Tuple, ", ", v => v.ToString());
                    return ZString.Concat("Tail:(", tupleStr, ")");
                }
                case DataType.UserData:
                    return "(UserData)";
                case DataType.Thread:
                    return ZString.Format("(Coroutine 0x{0:x})", Coroutine.ReferenceId);
                default:
                    return "(???)";
            }
        }

        /// <summary>
        /// Joins tuple elements into a string without LINQ allocations using ZString.
        /// </summary>
        private static string JoinTupleStrings(
            DynValue[] tuple,
            string separator,
            Func<DynValue, string> selector
        )
        {
            if (tuple == null || tuple.Length == 0)
            {
                return string.Empty;
            }

            if (tuple.Length == 1)
            {
                return selector(tuple[0]);
            }

            // Use notNested: false because the selector may recursively call ToString()
            // which could also use ZString, causing a nesting conflict
            using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder(notNested: false);
            sb.Append(selector(tuple[0]));
            for (int i = 1; i < tuple.Length; i++)
            {
                sb.Append(separator);
                sb.Append(selector(tuple[i]));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            DeterministicHashBuilder hash = default;
            hash.AddInt((int)Type);

            switch (Type)
            {
                case DataType.Void:
                case DataType.Nil:
                    return 0;
                case DataType.Boolean:
                    hash.AddInt(Boolean ? 1 : 0);
                    return hash.ToHashCode();
                case DataType.Number:
                    // Use LuaNumber's hash code to ensure equal numbers have equal hashes
                    hash.AddInt(
                        Number == 0.0 ? LuaNumber.Zero.GetHashCode() : LuaNumber.GetHashCode()
                    );
                    return hash.ToHashCode();
                case DataType.String:
                    hash.Add(String);
                    return hash.ToHashCode();
                case DataType.Function:
                    hash.Add(Function);
                    return hash.ToHashCode();
                case DataType.ClrFunction:
                    hash.Add(Callback);
                    return hash.ToHashCode();
                case DataType.Table:
                    hash.Add(Table);
                    return hash.ToHashCode();
                case DataType.Tuple:
                    hash.Add(Tuple);
                    return hash.ToHashCode();
                case DataType.TailCallRequest:
                case DataType.YieldRequest:
                    hash.Add(_object);
                    return hash.ToHashCode();
                case DataType.UserData:
                    if (UserData != null)
                    {
                        hash.AddInt(UserData.StableHashCode);
                    }
                    return hash.ToHashCode();
                case DataType.Thread:
                    if (Coroutine != null)
                    {
                        hash.AddInt(Coroutine.ReferenceId);
                    }
                    return hash.ToHashCode();
                default:
                    return hash.ToHashCode();
            }
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
            return obj is DynValue other && Equals(other);
        }

        /// <inheritdoc />
        public bool Equals(DynValue other)
        {
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
                    // Use LuaNumber comparison to preserve integer precision at boundaries
                    return LuaNumber.Equal(LuaNumber, other.LuaNumber);
                case DataType.String:
                    return String == other.String;
                case DataType.Function:
                    return Function == other.Function;
                case DataType.ClrFunction:
                    return Callback == other.Callback;
                case DataType.Table:
                    return Table == other.Table;
                case DataType.Tuple:
                    return Tuple == other.Tuple;
                case DataType.TailCallRequest:
                case DataType.YieldRequest:
                    return ReferenceEquals(_object, other._object);
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
                    return false;
            }
        }

        /// <summary>
        /// Determines whether two Lua values are equal.
        /// </summary>
        public static bool operator ==(DynValue left, DynValue right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two Lua values are not equal.
        /// </summary>
        public static bool operator !=(DynValue left, DynValue right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Casts this DynValue to string, using coercion if the type is number.
        /// Uses Lua 5.3+ formatting by default.
        /// </summary>
        /// <returns>The string representation, or null if not number, not string.</returns>
        public string CastToString()
        {
            // Default to Lua 5.3+ formatting for backwards compatibility
            return CastToString(LuaCompatibilityVersion.Lua53);
        }

        /// <summary>
        /// Casts this DynValue to string, using coercion if the type is number,
        /// with version-specific number formatting.
        /// </summary>
        /// <param name="version">The Lua compatibility version to use for number formatting.</param>
        /// <returns>The string representation, or null if not number, not string.</returns>
        /// <remarks>
        /// Number formatting differences by version:
        /// - Lua 5.1/5.2: Integer-like floats (e.g., 42.0) format as "42"
        /// - Lua 5.3+: Integer-like floats format as "42.0" to distinguish from integers
        /// </remarks>
        public string CastToString(LuaCompatibilityVersion version)
        {
            DynValue rv = ToScalar();
            if (rv.Type == DataType.Number)
            {
                // Use version-aware LuaNumber.ToLuaString() for correct number formatting
                return rv.LuaNumber.ToLuaString(version);
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
        /// Casts this DynValue to a <see cref="LuaNumber"/>, preserving integer/float subtyping.
        /// Uses coercion if the type is string.
        /// </summary>
        /// <returns>The LuaNumber value, or null if not number, not string or non-convertible-string.</returns>
        public LuaNumber? CastToLuaNumber()
        {
            DynValue rv = ToScalar();
            if (rv.Type == DataType.Number)
            {
                return rv.LuaNumber;
            }
            else if (rv.Type == DataType.String)
            {
                if (LuaNumber.TryParse(rv.String, out LuaNumber result))
                {
                    return result;
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

        private DynValue RequireFacadeType(DataType expected, string methodName)
        {
            if (_type != expected)
            {
                throw NewFacadeKindException(methodName, ToFacadeKind(expected, this), Kind);
            }

            return this;
        }

        private Script GetFacadeOwnerOrThrow()
        {
            Script ownerScript = this.GetOwnerScript();
            if (ownerScript == null)
            {
                throw new InvalidOperationException(
                    "Lua value is not owned by a LuaEngine instance."
                );
            }

            ownerScript.ThrowIfDisposed();
            return ownerScript;
        }

        private static InvalidOperationException NewFacadeKindException(
            string methodName,
            LuaKind expected,
            LuaKind actual
        )
        {
            return NewFacadeKindException(methodName, expected.ToString(), actual);
        }

        private static InvalidOperationException NewFacadeKindException(
            string methodName,
            string expected,
            LuaKind actual
        )
        {
            return new InvalidOperationException(
                string.Concat(methodName, " requires ", expected, " but found ", actual, ".")
            );
        }

        private static LuaKind ToFacadeKind(DynValue value)
        {
            return ToFacadeKind(value.Type, value);
        }

        private static LuaKind ToFacadeKind(DataType type, DynValue value)
        {
            switch (type)
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
