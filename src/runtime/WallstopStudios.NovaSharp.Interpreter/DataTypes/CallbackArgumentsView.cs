namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    /// <summary>
    /// Stack-only view of arguments received by an opt-in <see cref="CallbackFunction"/>.
    /// </summary>
    public readonly ref struct CallbackArgumentsView
    {
        private const int SourceFixed = 0;
        private const int SourceSpan = 1;
        private const int SourceList = 2;
        private const int SourceCallbackArguments = 3;

        private readonly ReadOnlySpan<DynValue> _span;
        private readonly IList<DynValue> _list;
        private readonly CallbackArguments _callbackArguments;
        private readonly DynValue _arg0;
        private readonly DynValue _arg1;
        private readonly DynValue _arg2;
        private readonly DynValue _arg3;
        private readonly DynValue _arg4;
        private readonly DynValue _arg5;
        private readonly DynValue _arg6;
        private readonly int _source;
        private readonly int _offset;
        private readonly int _storedCount;
        private readonly int _count;
        private readonly bool _lastIsTuple;
        private readonly bool _isMethodCall;

        internal CallbackArgumentsView(bool isMethodCall)
            : this(
                default,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                SourceFixed,
                0,
                0,
                isMethodCall
            ) { }

        internal CallbackArgumentsView(DynValue arg, bool isMethodCall)
            : this(
                default,
                null,
                null,
                arg,
                null,
                null,
                null,
                null,
                null,
                null,
                SourceFixed,
                0,
                1,
                isMethodCall
            ) { }

        internal CallbackArgumentsView(DynValue arg1, DynValue arg2, bool isMethodCall)
            : this(
                default,
                null,
                null,
                arg1,
                arg2,
                null,
                null,
                null,
                null,
                null,
                SourceFixed,
                0,
                2,
                isMethodCall
            ) { }

        internal CallbackArgumentsView(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            bool isMethodCall
        )
            : this(
                default,
                null,
                null,
                arg1,
                arg2,
                arg3,
                null,
                null,
                null,
                null,
                SourceFixed,
                0,
                3,
                isMethodCall
            ) { }

        internal CallbackArgumentsView(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            bool isMethodCall
        )
            : this(
                default,
                null,
                null,
                arg1,
                arg2,
                arg3,
                arg4,
                null,
                null,
                null,
                SourceFixed,
                0,
                4,
                isMethodCall
            ) { }

        internal CallbackArgumentsView(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            bool isMethodCall
        )
            : this(
                default,
                null,
                null,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5,
                null,
                null,
                SourceFixed,
                0,
                5,
                isMethodCall
            ) { }

        internal CallbackArgumentsView(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            bool isMethodCall
        )
            : this(
                default,
                null,
                null,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5,
                arg6,
                null,
                SourceFixed,
                0,
                6,
                isMethodCall
            ) { }

        internal CallbackArgumentsView(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7,
            bool isMethodCall
        )
            : this(
                default,
                null,
                null,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5,
                arg6,
                arg7,
                SourceFixed,
                0,
                7,
                isMethodCall
            ) { }

        /// <summary>
        /// Initializes a new argument view from contiguous backing storage.
        /// </summary>
        public CallbackArgumentsView(ReadOnlySpan<DynValue> args, bool isMethodCall)
            : this(
                args,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                SourceSpan,
                0,
                args.Length,
                isMethodCall
            ) { }

        /// <summary>
        /// Initializes a new argument view from list backing storage.
        /// </summary>
        public CallbackArgumentsView(IList<DynValue> args, bool isMethodCall)
            : this(args, 0, GetCountOrThrow(args), isMethodCall) { }

        /// <summary>
        /// Initializes a new argument view from a subrange of list backing storage.
        /// </summary>
        internal CallbackArgumentsView(
            IList<DynValue> args,
            int offset,
            int count,
            bool isMethodCall
        )
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (offset < 0 || count < 0 || offset > args.Count - count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            ReadOnlySpan<DynValue> span = default;
            bool hasSpan = false;
            if (args is DynValue[] array)
            {
                span = new ReadOnlySpan<DynValue>(array, offset, count);
                hasSpan = true;
            }
            else if (args is FastStack<DynValue> stack && stack.TryGetSpan(offset, count, out span))
            {
                hasSpan = true;
            }
            else if (args is Slice<DynValue> slice && slice.TryGetSpan(offset, count, out span))
            {
                hasSpan = true;
            }

            _span = span;
            _list = args;
            _callbackArguments = null;
            _arg0 = null;
            _arg1 = null;
            _arg2 = null;
            _arg3 = null;
            _arg4 = null;
            _arg5 = null;
            _arg6 = null;
            _source = hasSpan ? SourceSpan : SourceList;
            _offset = hasSpan ? 0 : offset;
            _storedCount = hasSpan ? count : offset + count;
            _isMethodCall = isMethodCall;

            int visibleStoredCount = _storedCount - _offset;
            if (visibleStoredCount <= 0)
            {
                _count = 0;
                _lastIsTuple = false;
            }
            else
            {
                DynValue last = GetStoredArgument(_storedCount - 1);
                if (last.Type == DataType.Tuple)
                {
                    _count = last.Tuple.Length - 1 + visibleStoredCount;
                    _lastIsTuple = true;
                }
                else if (last.Type == DataType.Void)
                {
                    _count = visibleStoredCount - 1;
                    _lastIsTuple = false;
                }
                else
                {
                    _count = visibleStoredCount;
                    _lastIsTuple = false;
                }
            }
        }

        private static int GetCountOrThrow(IList<DynValue> args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return args.Count;
        }

        /// <summary>
        /// Initializes a new argument view from a legacy callback argument container.
        /// </summary>
        public CallbackArgumentsView(CallbackArguments args)
            : this(
                default,
                null,
                args ?? throw new ArgumentNullException(nameof(args)),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                SourceCallbackArguments,
                0,
                args.Count,
                args.IsMethodCall
            ) { }

        private CallbackArgumentsView(
            ReadOnlySpan<DynValue> span,
            IList<DynValue> list,
            CallbackArguments callbackArguments,
            DynValue arg0,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            int source,
            int offset,
            int storedCount,
            bool isMethodCall
        )
        {
            _span = span;
            _list = list;
            _callbackArguments = callbackArguments;
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
            _arg5 = arg5;
            _arg6 = arg6;
            _source = source;
            _offset = offset;
            _storedCount = storedCount;
            _isMethodCall = isMethodCall;

            int visibleStoredCount = storedCount - offset;
            if (source == SourceCallbackArguments)
            {
                _count = storedCount;
                _lastIsTuple = false;
            }
            else if (visibleStoredCount <= 0)
            {
                _count = 0;
                _lastIsTuple = false;
            }
            else
            {
                DynValue last = GetStoredArgument(storedCount - 1);
                if (last.Type == DataType.Tuple)
                {
                    _count = last.Tuple.Length - 1 + visibleStoredCount;
                    _lastIsTuple = true;
                }
                else if (last.Type == DataType.Void)
                {
                    _count = visibleStoredCount - 1;
                    _lastIsTuple = false;
                }
                else
                {
                    _count = visibleStoredCount;
                    _lastIsTuple = false;
                }
            }
        }

        /// <summary>
        /// Gets the count of arguments visible to the callback.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Gets a value indicating whether this callback was invoked as a method call.
        /// </summary>
        public bool IsMethodCall
        {
            get { return _isMethodCall; }
        }

        /// <summary>
        /// Gets the <see cref="DynValue"/> at the specified index, or <see cref="DynValue.Void"/> if not found.
        /// </summary>
        public DynValue this[int index]
        {
            get { return RawGet(index, translateVoids: true) ?? DynValue.Void; }
        }

        /// <summary>
        /// Gets the <see cref="DynValue"/> at the specified index, or <c>null</c>.
        /// </summary>
        public DynValue RawGet(int index, bool translateVoids)
        {
            if (index < 0)
            {
                return null;
            }

            if (_source == SourceCallbackArguments)
            {
                return _callbackArguments.RawGet(index, translateVoids);
            }

            DynValue value;
            int visibleStoredCount = _storedCount - _offset;
            if (index >= _count)
            {
                return null;
            }

            if (!_lastIsTuple || index < visibleStoredCount - 1)
            {
                value = GetStoredArgument(_offset + index);
            }
            else
            {
                value =
                    GetStoredArgument(_storedCount - 1).Tuple[index - (visibleStoredCount - 1)]
                    ?? DynValue.Nil;
            }

            if (value.Type == DataType.Tuple)
            {
                value = value.Tuple.Length > 0 ? value.Tuple[0] ?? DynValue.Nil : DynValue.Nil;
            }

            if (translateVoids && value.Type == DataType.Void)
            {
                value = DynValue.Nil;
            }

            return value;
        }

        /// <summary>
        /// Converts the arguments to an array.
        /// </summary>
        public DynValue[] GetArray(int skip = 0)
        {
            if (skip >= _count)
            {
                return Array.Empty<DynValue>();
            }

            DynValue[] values = new DynValue[_count - skip];
            for (int i = skip; i < _count; i++)
            {
                values[i - skip] = this[i];
            }

            return values;
        }

        /// <summary>
        /// Returns a view where the first argument is skipped if this was a method call.
        /// </summary>
        public CallbackArgumentsView SkipMethodCall()
        {
            if (!_isMethodCall)
            {
                return this;
            }

            if (_source == SourceCallbackArguments)
            {
                return new CallbackArgumentsView(_callbackArguments.SkipMethodCall());
            }

            return new CallbackArgumentsView(
                _span,
                _list,
                null,
                _arg0,
                _arg1,
                _arg2,
                _arg3,
                _arg4,
                _arg5,
                _arg6,
                _source,
                Math.Min(_offset + 1, _storedCount),
                _storedCount,
                isMethodCall: false
            );
        }

        /// <summary>
        /// Tries to get a read-only span of the arguments when the backing storage is contiguous.
        /// </summary>
        public bool TryGetSpan(out ReadOnlySpan<DynValue> span)
        {
            if (_source == SourceCallbackArguments)
            {
                return _callbackArguments.TryGetSpan(out span);
            }

            if (_lastIsTuple || _source != SourceSpan)
            {
                span = default;
                return false;
            }

            span = _span.Slice(_offset, _count);
            if (ContainsArgumentNeedingNormalization(span))
            {
                span = default;
                return false;
            }

            return true;
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
        /// Copies arguments to a destination span.
        /// </summary>
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
        /// Copies arguments to a destination span, starting from the specified index.
        /// </summary>
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

        private DynValue GetStoredArgument(int index)
        {
            DynValue value = _source switch
            {
                SourceFixed => index switch
                {
                    0 => _arg0,
                    1 => _arg1,
                    2 => _arg2,
                    3 => _arg3,
                    4 => _arg4,
                    5 => _arg5,
                    6 => _arg6,
                    _ => throw new ArgumentOutOfRangeException(nameof(index)),
                },
                SourceSpan => _span[index],
                SourceList => _list[index],
                _ => throw new InvalidOperationException("Unsupported callback argument source."),
            };

            return value ?? DynValue.Nil;
        }
    }
}
