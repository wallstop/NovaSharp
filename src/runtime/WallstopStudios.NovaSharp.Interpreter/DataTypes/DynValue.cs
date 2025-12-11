namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using Compatibility;
    using Cysharp.Text;
    using DataStructs;
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
        private LuaNumber _number;
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
            return new DynValue()
            {
                _number = LuaNumber.FromInteger(v ? 1L : 0L),
                _type = DataType.Boolean,
            };
        }

        /// <summary>
        /// Returns a cached readonly boolean value. Use this instead of <see cref="NewBoolean"/>
        /// when a readonly value is acceptable (most common case in VM operations).
        /// </summary>
        /// <param name="value">The boolean value.</param>
        /// <returns>A cached readonly <see cref="DynValue"/> representing the boolean.</returns>
        public static DynValue FromBoolean(bool value)
        {
            return value ? True : False;
        }

        // Cache for small integers (0-255) commonly used as Lua array indices
        private static readonly DynValue[] SmallIntegerCache = InitializeSmallIntegerCache();

        private const int SmallIntegerCacheSize = 256;

        private static DynValue[] InitializeSmallIntegerCache()
        {
            DynValue[] cache = new DynValue[SmallIntegerCacheSize];
            for (int i = 0; i < SmallIntegerCacheSize; i++)
            {
                cache[i] = new DynValue()
                {
                    _number = LuaNumber.FromInteger(i),
                    _type = DataType.Number,
                    _readOnly = true,
                };
            }
            return cache;
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified number as a float subtype.
        /// </summary>
        public static DynValue NewNumber(double num)
        {
            return new DynValue()
            {
                _number = LuaNumber.FromDouble(num),
                _type = DataType.Number,
                _hashCode = -1,
            };
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified number with explicit float subtype.
        /// Unlike <see cref="NewNumber(double)"/>, this method preserves the float subtype even for
        /// whole numbers like 3.0, which is required for Lua 5.3+ compliance with numeric literals.
        /// </summary>
        public static DynValue NewFloat(double num)
        {
            return new DynValue()
            {
                _number = LuaNumber.FromFloat(num),
                _type = DataType.Number,
                _hashCode = -1,
            };
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified integer.
        /// The resulting value will have the Lua "integer" subtype.
        /// </summary>
        public static DynValue NewInteger(long num)
        {
            return new DynValue()
            {
                _number = LuaNumber.FromInteger(num),
                _type = DataType.Number,
                _hashCode = -1,
            };
        }

        /// <summary>
        /// Creates a new writable value initialized to the specified <see cref="LuaNumber"/>.
        /// </summary>
        public static DynValue NewNumber(LuaNumber num)
        {
            return new DynValue()
            {
                _number = num,
                _type = DataType.Number,
                _hashCode = -1,
            };
        }

        /// <summary>
        /// Returns a cached readonly number value for small non-negative integers (0-255).
        /// Falls back to <see cref="NewNumber(double)"/> for values outside the cache range.
        /// Use this in hot paths where readonly values are acceptable.
        /// </summary>
        /// <param name="num">The number value.</param>
        /// <returns>A cached or new <see cref="DynValue"/> representing the number.</returns>
        public static DynValue FromNumber(double num)
        {
            // Check if the number is a small non-negative integer in cache range
            int intVal = (int)num;
            if (intVal >= 0 && intVal < SmallIntegerCacheSize && intVal == num)
            {
                return SmallIntegerCache[intVal];
            }
            return NewNumber(num);
        }

        /// <summary>
        /// Returns a cached readonly number value for small non-negative integers (0-255).
        /// Falls back to <see cref="NewInteger(long)"/> for values outside the cache range.
        /// Use this in hot paths where readonly values are acceptable.
        /// The resulting value will have the Lua "integer" subtype.
        /// </summary>
        /// <param name="num">The integer value.</param>
        /// <returns>A cached or new <see cref="DynValue"/> representing the integer.</returns>
        public static DynValue FromInteger(long num)
        {
            // Check if the number is a small non-negative integer in cache range
            if (num >= 0 && num < SmallIntegerCacheSize)
            {
                return SmallIntegerCache[num];
            }
            return NewInteger(num);
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
            return new DynValue()
            {
                _object = ZString.Concat(left, right),
                _type = DataType.String,
            };
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
            return new DynValue() { _object = ZString.Concat(s1, s2, s3), _type = DataType.String };
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
            return new DynValue()
            {
                _object = ZString.Concat(s1, s2, s3, s4),
                _type = DataType.String,
            };
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
            return new DynValue() { _object = builder.ToString(), _type = DataType.String };
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
        /// Returns a DynValue wrapping the specified closure. This is an optimized path
        /// that reuses a DynValue if the closure already has one cached.
        /// </summary>
        /// <param name="closure">The closure to wrap.</param>
        /// <returns>A <see cref="DynValue"/> representing the closure.</returns>
        /// <remarks>
        /// This method checks if the closure has a cached DynValue and returns it if available,
        /// avoiding allocation in hot paths like coroutine creation.
        /// </remarks>
        internal static DynValue FromClosure(Closure closure)
        {
            if (closure == null)
            {
                return Nil;
            }

            DynValue cached = closure.CachedDynValue;
            if (cached != null)
            {
                return cached;
            }

            DynValue newValue = NewClosure(closure);
            closure.CachedDynValue = newValue;
            return newValue;
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
        /// Creates a new request for a tail call with no arguments.
        /// </summary>
        /// <param name="tailFn">The function to be called.</param>
        /// <returns></returns>
        public static DynValue NewTailCallReq(DynValue tailFn)
        {
            return new DynValue()
            {
                _object = new TailCallData() { Args = Array.Empty<DynValue>(), Function = tailFn },
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
            return new DynValue() { _object = new[] { value1, value2 }, _type = DataType.Tuple };
        }

        /// <summary>
        /// Creates a new tuple initialized to three values.
        /// This is an optimized overload that avoids params array allocation.
        /// </summary>
        public static DynValue NewTuple(DynValue value1, DynValue value2, DynValue value3)
        {
            return new DynValue()
            {
                _object = new[] { value1, value2, value3 },
                _type = DataType.Tuple,
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
        /// Creates a new tuple initialized to a single value - which can be potentially a tuple.
        /// Returns the value directly (tuple flattening).
        /// </summary>
        public static DynValue NewTupleNested(DynValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value;
        }

        /// <summary>
        /// Creates a new tuple initialized to two values - which can be potentially other tuples.
        /// This is an optimized overload that avoids params array allocation.
        /// </summary>
        public static DynValue NewTupleNested(DynValue value1, DynValue value2)
        {
            if (value1 == null)
            {
                throw new ArgumentNullException(nameof(value1));
            }

            if (value2 == null)
            {
                throw new ArgumentNullException(nameof(value2));
            }

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

                return new DynValue()
                {
                    _object = ListPool<DynValue>.ToExactArray(vals),
                    _type = DataType.Tuple,
                };
            }
        }

        /// <summary>
        /// Creates a new tuple initialized to three values - which can be potentially other tuples.
        /// This is an optimized overload that avoids params array allocation.
        /// </summary>
        public static DynValue NewTupleNested(DynValue value1, DynValue value2, DynValue value3)
        {
            if (value1 == null)
            {
                throw new ArgumentNullException(nameof(value1));
            }

            if (value2 == null)
            {
                throw new ArgumentNullException(nameof(value2));
            }

            if (value3 == null)
            {
                throw new ArgumentNullException(nameof(value3));
            }

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

                return new DynValue()
                {
                    _object = ListPool<DynValue>.ToExactArray(vals),
                    _type = DataType.Tuple,
                };
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

                return new DynValue()
                {
                    _object = ListPool<DynValue>.ToExactArray(vals),
                    _type = DataType.Tuple,
                };
            }
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

        /// <summary>
        /// A preinitialized, readonly instance, equaling an empty string
        /// </summary>
        public static DynValue EmptyString { get; private set; }

        static DynValue()
        {
            Nil = new DynValue() { _type = DataType.Nil }.AsReadOnly();
            Void = new DynValue() { _type = DataType.Void }.AsReadOnly();
            True = NewBoolean(true).AsReadOnly();
            False = NewBoolean(false).AsReadOnly();
            EmptyString = NewString(string.Empty).AsReadOnly();
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
                    return "nil";
                case DataType.Boolean:
                    return Boolean ? "true" : "false";
                case DataType.Number:
                    return Number.ToString(CultureInfo.InvariantCulture);
                case DataType.String:
                    // Use ZString.Concat for zero-allocation string building.
                    // JoinTupleStrings already uses notNested: false so recursive calls are safe.
                    return ZString.Concat("\"", String, "\"");
                case DataType.Function:
                    return ZString.Format("(Function {0:X8})", Function.EntryPointByteCodeLocation);
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
                    return ZString.Format("(Coroutine {0:X8})", Coroutine.ReferenceId);
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
            if (_hashCode != -1)
            {
                return _hashCode;
            }

            DeterministicHashBuilder hash = default;
            hash.AddInt((int)Type);

            switch (Type)
            {
                case DataType.Void:
                case DataType.Nil:
                    _hashCode = 0;
                    break;
                case DataType.Boolean:
                    hash.AddInt(Boolean ? 1 : 0);
                    _hashCode = hash.ToHashCode();
                    break;
                case DataType.Number:
                    // Use LuaNumber's hash code to ensure equal numbers have equal hashes
                    hash.AddInt(LuaNumber.GetHashCode());
                    _hashCode = hash.ToHashCode();
                    break;
                case DataType.String:
                    hash.Add(String);
                    _hashCode = hash.ToHashCode();
                    break;
                case DataType.Function:
                    hash.Add(Function);
                    _hashCode = hash.ToHashCode();
                    break;
                case DataType.ClrFunction:
                    hash.Add(Callback);
                    _hashCode = hash.ToHashCode();
                    break;
                case DataType.Table:
                    hash.Add(Table);
                    _hashCode = hash.ToHashCode();
                    break;
                case DataType.Tuple:
                case DataType.TailCallRequest:
                    hash.Add(Tuple);
                    _hashCode = hash.ToHashCode();
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

            _number = LuaNumber.FromDouble(num);
        }

        /// <summary>
        /// Changes the numeric value of a number DynValue using a <see cref="LuaNumber"/>.
        /// </summary>
        internal void AssignNumber(LuaNumber num)
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
