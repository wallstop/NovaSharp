namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;

    /// <summary>
    /// This class is a container for arguments received by a CallbackFunction
    /// </summary>
    public class CallbackArguments
    {
        private readonly IList<DynValue> _args;
        private readonly int _count;
        private bool _lastIsTuple;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallbackArguments" /> class.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="isMethodCall">if set to <c>true</c> [is method call].</param>
        public CallbackArguments(IList<DynValue> args, bool isMethodCall)
        {
            _args = args;

            if (_args.Count > 0)
            {
                DynValue last = _args[^1];

                if (last.Type == DataType.Tuple)
                {
                    _count = last.Tuple.Length - 1 + _args.Count;
                    _lastIsTuple = true;
                }
                else if (last.Type == DataType.Void)
                {
                    _count = _args.Count - 1;
                }
                else
                {
                    _count = _args.Count;
                }
            }
            else
            {
                _count = 0;
            }

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

            if (index >= _count)
            {
                return null;
            }

            if (!_lastIsTuple || index < _args.Count - 1)
            {
                v = _args[index];
            }
            else
            {
                v = _args[^1].Tuple[index - (_args.Count - 1)];
            }

            if (v.Type == DataType.Tuple)
            {
                if (v.Tuple.Length > 0)
                {
                    v = v.Tuple[0];
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

            if (
                (this[argNum].Type == DataType.Table)
                && (this[argNum].Table.MetaTable != null)
                && (this[argNum].Table.MetaTable.RawGet("__tostring") != null)
            )
            {
                DynValue v = executionContext.Script.Call(
                    this[argNum].Table.MetaTable.RawGet("__tostring"),
                    this[argNum]
                );

                if (v.Type != DataType.String)
                {
                    throw new ScriptRuntimeException(
                        "'tostring' must return a string to '{0}'",
                        funcName
                    );
                }

                return v.ToPrintString();
            }
            else
            {
                return (this[argNum].ToPrintString());
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
                Slice<DynValue> slice = new(_args, 1, _args.Count - 1, false);
                return new CallbackArguments(slice, false);
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
        /// <returns><c>true</c> if the span could be obtained; <c>false</c> if the backing is not contiguous or contains tuple expansion.</returns>
        /// <remarks>
        /// This method only succeeds when the arguments are stored in a contiguous array or list
        /// and there is no tuple expansion at the end. Use this in hot paths where avoiding
        /// allocation is critical.
        /// </remarks>
        public bool TryGetSpan(out ReadOnlySpan<DynValue> span)
        {
            // Cannot return span if last argument is a tuple (would need to expand it)
            if (_lastIsTuple)
            {
                span = default;
                return false;
            }

            // Try to get span from different backing types
            if (_args is DynValue[] array)
            {
                span = new ReadOnlySpan<DynValue>(array, 0, _count);
                return true;
            }

            if (_args is List<DynValue> list)
            {
                // List<T> doesn't directly expose its array, but we can use CollectionsMarshal in .NET 5+
                // For now, fall back to false for lists
                span = default;
                return false;
            }

            span = default;
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
