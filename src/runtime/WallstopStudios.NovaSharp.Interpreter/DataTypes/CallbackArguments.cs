namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;

    /// <summary>
    /// This class is a container for arguments received by a CallbackFunction
    /// </summary>
    public class CallbackArguments
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct FixedArgumentStorage
        {
            internal DynValue _arg0;
            internal DynValue _arg1;
            internal DynValue _arg2;
            internal DynValue _arg3;
            internal DynValue _arg4;

            internal FixedArgumentStorage(DynValue arg0)
            {
                _arg0 = arg0;
                _arg1 = null;
                _arg2 = null;
                _arg3 = null;
                _arg4 = null;
            }

            internal FixedArgumentStorage(DynValue arg0, DynValue arg1)
            {
                _arg0 = arg0;
                _arg1 = arg1;
                _arg2 = null;
                _arg3 = null;
                _arg4 = null;
            }

            internal FixedArgumentStorage(DynValue arg0, DynValue arg1, DynValue arg2)
            {
                _arg0 = arg0;
                _arg1 = arg1;
                _arg2 = arg2;
                _arg3 = null;
                _arg4 = null;
            }

            internal FixedArgumentStorage(
                DynValue arg0,
                DynValue arg1,
                DynValue arg2,
                DynValue arg3
            )
            {
                _arg0 = arg0;
                _arg1 = arg1;
                _arg2 = arg2;
                _arg3 = arg3;
                _arg4 = null;
            }

            internal FixedArgumentStorage(
                DynValue arg0,
                DynValue arg1,
                DynValue arg2,
                DynValue arg3,
                DynValue arg4
            )
            {
                _arg0 = arg0;
                _arg1 = arg1;
                _arg2 = arg2;
                _arg3 = arg3;
                _arg4 = arg4;
            }

            /// <summary>
            /// Gets the raw fixed argument stored at the specified index.
            /// </summary>
            internal readonly DynValue Get(int index)
            {
                return index switch
                {
                    0 => _arg0,
                    1 => _arg1,
                    2 => _arg2,
                    3 => _arg3,
                    4 => _arg4,
                    _ => null,
                };
            }

            /// <summary>
            /// Exposes the leading fixed arguments as a contiguous read-only span.
            /// </summary>
            internal ReadOnlySpan<DynValue> AsSpan(int count)
            {
                return MemoryMarshal.CreateReadOnlySpan(ref _arg0, count);
            }
        }

        private readonly IList<DynValue> _args;
        private FixedArgumentStorage _fixedArgs;
        private readonly int _count;
        private readonly int _fixedCount;
        private bool _lastIsTuple;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallbackArguments" /> class.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="isMethodCall">if set to <c>true</c> [is method call].</param>
        public CallbackArguments(IList<DynValue> args, bool isMethodCall)
        {
            _args = args;
            _fixedArgs = default;
            _fixedCount = 0;

            if (_args.Count > 0)
            {
                DynValue last = _args[^1] ?? DynValue.Nil;

                _count = CalculateExpandedCount(_args.Count, last, out _lastIsTuple);
            }
            else
            {
                _count = 0;
            }

            IsMethodCall = isMethodCall;
        }

        internal CallbackArguments(bool isMethodCall)
        {
            _args = null;
            _fixedArgs = default;
            _fixedCount = 0;
            _count = 0;
            _lastIsTuple = false;
            IsMethodCall = isMethodCall;
        }

        internal CallbackArguments(DynValue arg, bool isMethodCall)
        {
            _args = null;
            _fixedArgs = new FixedArgumentStorage(arg);
            _fixedCount = 1;
            _count = CalculateExpandedCount(
                _fixedCount,
                _fixedArgs._arg0 ?? DynValue.Nil,
                out _lastIsTuple
            );
            IsMethodCall = isMethodCall;
        }

        internal CallbackArguments(DynValue arg1, DynValue arg2, bool isMethodCall)
        {
            _args = null;
            _fixedArgs = new FixedArgumentStorage(arg1, arg2);
            _fixedCount = 2;
            _count = CalculateExpandedCount(
                _fixedCount,
                _fixedArgs._arg1 ?? DynValue.Nil,
                out _lastIsTuple
            );
            IsMethodCall = isMethodCall;
        }

        internal CallbackArguments(DynValue arg1, DynValue arg2, DynValue arg3, bool isMethodCall)
        {
            _args = null;
            _fixedArgs = new FixedArgumentStorage(arg1, arg2, arg3);
            _fixedCount = 3;
            _count = CalculateExpandedCount(
                _fixedCount,
                _fixedArgs._arg2 ?? DynValue.Nil,
                out _lastIsTuple
            );
            IsMethodCall = isMethodCall;
        }

        internal CallbackArguments(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            bool isMethodCall
        )
        {
            _args = null;
            _fixedArgs = new FixedArgumentStorage(arg1, arg2, arg3, arg4);
            _fixedCount = 4;
            _count = CalculateExpandedCount(
                _fixedCount,
                _fixedArgs._arg3 ?? DynValue.Nil,
                out _lastIsTuple
            );
            IsMethodCall = isMethodCall;
        }

        internal CallbackArguments(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            bool isMethodCall
        )
        {
            _args = null;
            _fixedArgs = new FixedArgumentStorage(arg1, arg2, arg3, arg4, arg5);
            _fixedCount = 5;
            _count = CalculateExpandedCount(
                _fixedCount,
                _fixedArgs._arg4 ?? DynValue.Nil,
                out _lastIsTuple
            );
            IsMethodCall = isMethodCall;
        }

        /// <summary>
        /// Gets the count of arguments
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is a method call.
        /// </summary>
        public bool IsMethodCall { get; private set; }

        /// <summary>
        /// Gets the <see cref="DynValue"/> at the specified index, or Void if not found
        /// </summary>
        public DynValue this[int index]
        {
            get { return RawGet(index, true) ?? DynValue.Void; }
        }

        /// <summary>
        /// Gets the <see cref="DynValue" /> at the specified index, or null.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="translateVoids">if set to <c>true</c> all voids are translated to nils.</param>
        /// <returns></returns>
        public DynValue RawGet(int index, bool translateVoids)
        {
            DynValue v;

            if (index < 0 || index >= _count)
            {
                return null;
            }

            if (_args == null)
            {
                if (!_lastIsTuple || index < _fixedCount - 1)
                {
                    v = GetFixedArgument(index);
                }
                else
                {
                    v =
                        GetFixedArgument(_fixedCount - 1).Tuple[index - (_fixedCount - 1)]
                        ?? DynValue.Nil;
                }
            }
            else
            {
                if (!_lastIsTuple || index < _args.Count - 1)
                {
                    v = _args[index] ?? DynValue.Nil;
                }
                else
                {
                    v = _args[^1].Tuple[index - (_args.Count - 1)] ?? DynValue.Nil;
                }
            }

            if (v.Type == DataType.Tuple)
            {
                if (v.Tuple.Length > 0)
                {
                    v = v.Tuple[0] ?? DynValue.Nil;
                }
                else
                {
                    v = DynValue.Nil;
                }
            }

            if (translateVoids && v.Type == DataType.Void)
            {
                v = DynValue.Nil;
            }

            return v;
        }

        private static int CalculateExpandedCount(
            int storedCount,
            DynValue last,
            out bool lastIsTuple
        )
        {
            lastIsTuple = false;

            if (last.Type == DataType.Tuple)
            {
                lastIsTuple = true;
                return last.Tuple.Length - 1 + storedCount;
            }

            if (last.Type == DataType.Void)
            {
                return storedCount - 1;
            }

            return storedCount;
        }

        private DynValue GetFixedArgument(int index)
        {
            DynValue value = _fixedArgs.Get(index);
            return value ?? DynValue.Nil;
        }

        /// <summary>
        /// Converts the arguments to an array
        /// </summary>
        /// <param name="skip">The number of elements to skip (default= 0).</param>
        /// <returns></returns>
        public DynValue[] GetArray(int skip = 0)
        {
            if (skip >= _count)
            {
                return Array.Empty<DynValue>();
            }

            DynValue[] vals = new DynValue[_count - skip];

            for (int i = skip; i < _count; i++)
            {
                vals[i - skip] = this[i];
            }

            return vals;
        }

        /// <summary>
        /// Gets the specified argument as as an argument of the specified type. If not possible,
        /// an exception is raised.
        /// </summary>
        /// <param name="argNum">The argument number.</param>
        /// <param name="funcName">Name of the function.</param>
        /// <param name="type">The type desired.</param>
        /// <param name="allowNil">if set to <c>true</c> nil values are allowed.</param>
        /// <returns></returns>
        public DynValue AsType(int argNum, string funcName, DataType type, bool allowNil = false)
        {
            return this[argNum]
                .CheckType(
                    funcName,
                    type,
                    argNum,
                    allowNil
                        ? TypeValidationOptions.AllowNil | TypeValidationOptions.AutoConvert
                        : TypeValidationOptions.AutoConvert
                );
        }

        /// <summary>
        /// Gets the specified argument as as an argument of the specified user data type. If not possible,
        /// an exception is raised.
        /// </summary>
        /// <typeparam name="T">The desired userdata type</typeparam>
        /// <param name="argNum">The argument number.</param>
        /// <param name="funcName">Name of the function.</param>
        /// <param name="allowNil">if set to <c>true</c> nil values are allowed.</param>
        /// <returns></returns>
        public T AsUserData<T>(int argNum, string funcName, bool allowNil = false)
        {
            return this[argNum]
                .CheckUserDataType<T>(
                    funcName,
                    argNum,
                    allowNil ? TypeValidationOptions.AllowNil : default
                );
        }

        /// <summary>
        /// Gets the specified argument as an integer
        /// </summary>
        /// <param name="argNum">The argument number.</param>
        /// <param name="funcName">Name of the function.</param>
        /// <returns></returns>
        public int AsInt(int argNum, string funcName)
        {
            DynValue v = AsType(argNum, funcName, DataType.Number, false);
            double d = v.Number;
            return (int)d;
        }

        /// <summary>
        /// Gets the specified argument as a long integer
        /// </summary>
        /// <param name="argNum">The argument number.</param>
        /// <param name="funcName">Name of the function.</param>
        /// <returns></returns>
        public long AsLong(int argNum, string funcName)
        {
            DynValue v = AsType(argNum, funcName, DataType.Number, false);
            double d = v.Number;
            return (long)d;
        }

        /// <summary>
        /// Gets the specified argument as a string, calling the __tostring metamethod if needed, in a NON
        /// yield-compatible way.
        /// </summary>
        /// <param name="executionContext">The execution context.</param>
        /// <param name="argNum">The argument number.</param>
        /// <param name="funcName">Name of the function.</param>
        /// <returns></returns>
        /// <exception cref="ScriptRuntimeException">'tostring' must return a string to '{0}'</exception>
        public string AsStringUsingMeta(
            ScriptExecutionContext executionContext,
            int argNum,
            string funcName
        )
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            // Get the Lua version for version-aware number formatting
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            DynValue argument = this[argNum];
            if ((argument.Type == DataType.Table) && (argument.Table.MetaTable != null))
            {
                DynValue stringMeta = argument.Table.MetaTable.RawGet(Metamethods.ToStringMeta);
                if (stringMeta == null)
                {
                    return argument.ToPrintString(version);
                    ;
                }

                DynValue v = executionContext.Script.Call(stringMeta, argument);

                if (v.Type != DataType.String)
                {
                    throw new ScriptRuntimeException(
                        "'tostring' must return a string to '{0}'",
                        funcName
                    );
                }

                return v.ToPrintString(version);
            }
            else
            {
                return argument.ToPrintString(version);
            }
        }

        /// <summary>
        /// Returns a copy of CallbackArguments where the first ("self") argument is skipped if this was a method call,
        /// otherwise returns itself.
        /// </summary>
        /// <returns></returns>
        public CallbackArguments SkipMethodCall()
        {
            if (IsMethodCall)
            {
                if (_args != null)
                {
                    Slice<DynValue> slice = new(_args, 1, _args.Count - 1, false);
                    return new CallbackArguments(slice, false);
                }

                switch (_fixedCount)
                {
                    case 0:
                    case 1:
                        return new CallbackArguments(false);
                    case 2:
                        return new CallbackArguments(_fixedArgs._arg1, false);
                    case 3:
                        return new CallbackArguments(_fixedArgs._arg1, _fixedArgs._arg2, false);
                    case 4:
                        return new CallbackArguments(
                            _fixedArgs._arg1,
                            _fixedArgs._arg2,
                            _fixedArgs._arg3,
                            false
                        );
                    case 5:
                        return new CallbackArguments(
                            _fixedArgs._arg1,
                            _fixedArgs._arg2,
                            _fixedArgs._arg3,
                            _fixedArgs._arg4,
                            false
                        );
                    default:
                        throw new InvalidOperationException("Invalid fixed argument count.");
                }
            }
            else
            {
                return this;
            }
        }

        /// <summary>
        /// Tries to get a read-only span of the arguments when the backing storage is contiguous.
        /// </summary>
        /// <param name="span">When successful, contains the arguments as a span.</param>
        /// <returns><c>true</c> if the span could be obtained; <c>false</c> if the backing is not contiguous, contains tuple expansion, or requires null-to-nil normalization.</returns>
        /// <remarks>
        /// This method only succeeds when the arguments are stored in contiguous array-backed or
        /// fixed-field storage and there is no tuple expansion or null normalization required.
        /// Use this in hot paths where avoiding allocation is critical.
        /// </remarks>
        public bool TryGetSpan(out ReadOnlySpan<DynValue> span)
        {
            // Cannot return span if last argument is a tuple (would need to expand it)
            if (_lastIsTuple)
            {
                span = default;
                return false;
            }

            if (_args == null)
            {
                if (_count == 0)
                {
                    span = ReadOnlySpan<DynValue>.Empty;
                    return true;
                }

                span = _fixedArgs.AsSpan(_count);
                if (ContainsArgumentNeedingNormalization(span))
                {
                    span = default;
                    return false;
                }

                return true;
            }

            // Try to get span from different backing types
            if (_args is DynValue[] array)
            {
                span = new ReadOnlySpan<DynValue>(array, 0, _count);
                if (ContainsArgumentNeedingNormalization(span))
                {
                    span = default;
                    return false;
                }

                return true;
            }

            if (_args is Slice<DynValue> slice && slice.TryGetSpan(0, _count, out span))
            {
                if (ContainsArgumentNeedingNormalization(span))
                {
                    span = default;
                    return false;
                }

                return true;
            }

            if (_args is List<DynValue>)
            {
                span = default;
                return false;
            }

            span = default;
            return false;
        }

        private static bool ContainsArgumentNeedingNormalization(ReadOnlySpan<DynValue> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                DynValue value = span[i];
                if (value == null || value.Type == DataType.Tuple || value.Type == DataType.Void)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Copies the arguments to a destination span.
        /// </summary>
        /// <param name="destination">The span to copy arguments into.</param>
        /// <returns>The number of arguments copied.</returns>
        /// <remarks>
        /// This method handles tuple expansion correctly and is suitable when <see cref="TryGetSpan"/>
        /// returns <c>false</c>. It does not allocate.
        /// </remarks>
        public int CopyTo(Span<DynValue> destination)
        {
            int toCopy = Math.Min(_count, destination.Length);
            for (int i = 0; i < toCopy; i++)
            {
                destination[i] = this[i];
            }
            return toCopy;
        }

        /// <summary>
        /// Copies the arguments to a destination span, starting from the specified index.
        /// </summary>
        /// <param name="destination">The span to copy arguments into.</param>
        /// <param name="skip">The number of leading arguments to skip.</param>
        /// <returns>The number of arguments copied.</returns>
        public int CopyTo(Span<DynValue> destination, int skip)
        {
            if (skip >= _count)
            {
                return 0;
            }

            int toCopy = Math.Min(_count - skip, destination.Length);
            for (int i = 0; i < toCopy; i++)
            {
                destination[i] = this[i + skip];
            }
            return toCopy;
        }

        /// <summary>
        /// Gets a pooled array containing the arguments.
        /// </summary>
        /// <param name="array">The array containing the arguments.</param>
        /// <param name="skip">The number of leading arguments to skip.</param>
        /// <returns>A <see cref="PooledResource{T}"/> that must be disposed to return the array to the pool.</returns>
        /// <remarks>
        /// Use this method when you need to pass arguments to methods that require an array,
        /// but want to avoid allocating a new array for each call.
        /// <code>
        /// using (PooledResource&lt;DynValue[]&gt; pooled = args.GetPooledArray(out DynValue[] array))
        /// {
        ///     // Use array...
        /// }
        /// </code>
        /// </remarks>
        internal DataStructs.PooledResource<DynValue[]> GetPooledArray(
            out DynValue[] array,
            int skip = 0
        )
        {
            int count = _count - skip;
            if (count <= 0)
            {
                array = Array.Empty<DynValue>();
                return new DataStructs.PooledResource<DynValue[]>(array, _ => { });
            }

            DataStructs.PooledResource<DynValue[]> pooled = DataStructs.DynValueArrayPool.Get(
                count,
                out array
            );
            CopyTo(new Span<DynValue>(array, 0, count), skip);
            return pooled;
        }
    }
}
