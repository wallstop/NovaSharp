namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    /// <summary>
    /// A class representing a script function
    /// </summary>
    public class Closure : RefIdObject, IScriptPrivateResource
    {
        // Estimated base memory overhead for an empty Closure (object header, fields, ClosureContext reference).
        // Conservative estimate: object header (16-24 bytes) + fields (int + refs) + ClosureContext overhead.
        private const int BaseClosureOverhead = 128;

        // Estimated overhead per captured upvalue in the ClosureContext.
        private const int PerUpValueOverhead = 16;

        /// <summary>
        /// Type of closure based on upvalues
        /// </summary>
        public enum UpValuesType
        {
            /// <summary>
            /// The closure has no upvalues (thus, technically, it's a function and not a closure!)
            /// </summary>
            [Obsolete("Prefer explicit UpValuesType.", false)]
            None = 0,

            /// <summary>
            /// The closure has _ENV as its only upvalue
            /// </summary>
            Environment = 1,

            /// <summary>
            /// The closure is a "real" closure, with multiple upvalues
            /// </summary>
            Closure = 2,
        }

        /// <summary>
        /// Gets the entry point location in bytecode .
        /// </summary>
        public int EntryPointByteCodeLocation { get; private set; }

        /// <summary>
        /// Gets the script owning this function
        /// </summary>
        public Script OwnerScript { get; private set; }

        /// <summary>
        /// Shortcut for an empty closure
        /// </summary>
        private static readonly ClosureContext EmptyClosure = new();

        /// <summary>
        /// The current closure context
        /// </summary>
        internal ClosureContext ClosureContext { get; private set; }

        /// <summary>
        /// Gets a read-only view of the captured upvalues for this closure.
        /// </summary>
        public IReadOnlyList<DynValue> Context
        {
            get { return ClosureContext; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Closure"/> class from a list of captured cells.
        /// This overload avoids enumerator allocation by using the list directly.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="idx">The bytecode entry point index.</param>
        /// <param name="symbols">The symbol references for upvalues.</param>
        /// <param name="resolvedLocals">The captured local/upvalue cells.</param>
        internal Closure(
            Script script,
            int idx,
            SymbolRef[] symbols,
            List<ValueSlot> resolvedLocals
        )
        {
            OwnerScript = script;
            EntryPointByteCodeLocation = idx;

            if (symbols.Length > 0)
            {
                ClosureContext = new ClosureContext(symbols, resolvedLocals);
            }
            else
            {
                ClosureContext = EmptyClosure;
            }

            TrackAllocation(script, symbols.Length);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Closure"/> class from an array of captured cells.
        /// This overload avoids enumerator allocation entirely.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="idx">The bytecode entry point index.</param>
        /// <param name="symbols">The symbol references for upvalues.</param>
        /// <param name="resolvedLocals">The captured local/upvalue cells.</param>
        internal Closure(Script script, int idx, SymbolRef[] symbols, ValueSlot[] resolvedLocals)
        {
            OwnerScript = script;
            EntryPointByteCodeLocation = idx;

            if (symbols.Length > 0)
            {
                ClosureContext = new ClosureContext(symbols, resolvedLocals);
            }
            else
            {
                ClosureContext = EmptyClosure;
            }

            TrackAllocation(script, symbols.Length);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Closure"/> class with a single _ENV upvalue.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="idx">The bytecode entry point index.</param>
        /// <param name="environmentValue">The initial environment value for this closure.</param>
        internal Closure(Script script, int idx, DynValue environmentValue)
        {
            OwnerScript = script;
            EntryPointByteCodeLocation = idx;
            ClosureContext = new ClosureContext(environmentValue);
            TrackAllocation(script, 1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Closure"/> class with an existing closure context.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="idx">The bytecode entry point index.</param>
        /// <param name="closureContext">The closure context to reuse.</param>
        internal Closure(Script script, int idx, ClosureContext closureContext)
        {
            OwnerScript = script;
            EntryPointByteCodeLocation = idx;
            ClosureContext = closureContext ?? EmptyClosure;
            TrackAllocation(script, ClosureContext.Count);
        }

        /// <summary>
        /// Calls this function with the specified args
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call()
        {
            return OwnerScript.Call(DynValue.FromClosure(this));
        }

        /// <summary>
        /// Calls this function with one CLR object argument.
        /// </summary>
        /// <param name="arg">The argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object arg)
        {
            return OwnerScript.Call(this, arg);
        }

        /// <summary>
        /// Calls this function with two CLR object arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object arg1, object arg2)
        {
            return OwnerScript.Call(this, arg1, arg2);
        }

        /// <summary>
        /// Calls this function with three CLR object arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object arg1, object arg2, object arg3)
        {
            return OwnerScript.Call(this, arg1, arg2, arg3);
        }

        /// <summary>
        /// Calls this function with four CLR object arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object arg1, object arg2, object arg3, object arg4)
        {
            return OwnerScript.Call(this, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// Calls this function with five CLR object arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            return OwnerScript.Call(this, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// Calls this function with six CLR object arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <param name="arg6">The sixth argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5,
            object arg6
        )
        {
            return OwnerScript.Call(this, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// Calls this function with seven CLR object arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <param name="arg6">The sixth argument to pass to the function.</param>
        /// <param name="arg7">The seventh argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5,
            object arg6,
            object arg7
        )
        {
            return OwnerScript.Call(this, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// Calls this function with one pre-created DynValue argument.
        /// </summary>
        /// <param name="arg">The argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue arg)
        {
            return OwnerScript.Call(DynValue.FromClosure(this), arg);
        }

        /// <summary>
        /// Calls this function with two pre-created DynValue arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue arg1, DynValue arg2)
        {
            return OwnerScript.Call(DynValue.FromClosure(this), arg1, arg2);
        }

        /// <summary>
        /// Calls this function with three pre-created DynValue arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue arg1, DynValue arg2, DynValue arg3)
        {
            return OwnerScript.Call(DynValue.FromClosure(this), arg1, arg2, arg3);
        }

        /// <summary>
        /// Calls this function with four pre-created DynValue arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4)
        {
            return OwnerScript.Call(DynValue.FromClosure(this), arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// Calls this function with five pre-created DynValue arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5
        )
        {
            return OwnerScript.Call(DynValue.FromClosure(this), arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// Calls this function with six pre-created DynValue arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <param name="arg6">The sixth argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6
        )
        {
            return OwnerScript.Call(DynValue.FromClosure(this), arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// Calls this function with seven pre-created DynValue arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <param name="arg6">The sixth argument to pass to the function.</param>
        /// <param name="arg7">The seventh argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7
        )
        {
            return OwnerScript.Call(
                DynValue.FromClosure(this),
                arg1,
                arg2,
                arg3,
                arg4,
                arg5,
                arg6,
                arg7
            );
        }

        /// <summary>
        /// Calls this function with caller-owned contiguous DynValue arguments.
        /// </summary>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(ReadOnlySpan<DynValue> args)
        {
            return OwnerScript.Call(DynValue.FromClosure(this), args);
        }

        /// <summary>
        /// Calls this function with caller-owned CLR object argument storage.
        /// </summary>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not callable.</exception>
        public DynValue CallObjectArguments(object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return CallObjectArguments(args.AsSpan());
        }

        /// <summary>
        /// Calls this function with caller-owned contiguous CLR object arguments.
        /// </summary>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not callable.</exception>
        public DynValue CallObjectArguments(ReadOnlySpan<object> args)
        {
            return OwnerScript.CallObjectArguments(DynValue.FromClosure(this), args);
        }

        /// <summary>
        /// Calls this function with the specified args
        /// </summary>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(params object[] args)
        {
            return OwnerScript.Call(this, args);
        }

        /// <summary>
        /// Calls this function with the specified args
        /// </summary>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(params DynValue[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return OwnerScript.Call(DynValue.FromClosure(this), args);
        }

        /// <summary>
        /// Gets a delegate wrapping calls to this scripted function
        /// </summary>
        /// <returns></returns>
        public ScriptFunctionCallback GetDelegate()
        {
            return args => Call(args).ToObject();
        }

        /// <summary>
        /// Gets a delegate wrapping calls to this scripted function
        /// </summary>
        /// <typeparam name="T">The type of return value of the delegate.</typeparam>
        /// <returns></returns>
        public ScriptFunctionCallback<T> GetDelegate<T>()
        {
            return args => Call(args).ToObject<T>();
        }

        /// <summary>
        /// Gets the number of upvalues in this closure.
        /// </summary>
        public int UpValuesCount
        {
            get { return ClosureContext.Count; }
        }

        /// <summary>
        /// Gets the name of the specified upvalue.
        /// </summary>
        /// <param name="idx">The index of the upvalue.</param>
        /// <returns>The upvalue name</returns>
        public string GetUpValueName(int idx)
        {
            return ClosureContext.Symbols[idx];
        }

        /// <summary>
        /// Gets the current value of an upvalue.
        /// To set the value, use <see cref="SetUpValue(int, DynValue)"/>.
        /// </summary>
        /// <param name="idx">The index of the upvalue.</param>
        /// <returns>The upvalue value.</returns>
        public DynValue GetUpValue(int idx)
        {
            return ClosureContext[idx];
        }

        /// <summary>
        /// Sets the value of an upvalue.
        /// </summary>
        /// <param name="idx">The index of the upvalue.</param>
        /// <param name="value">The value to assign to the upvalue.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if idx is out of range.</exception>
        public void SetUpValue(int idx, DynValue value)
        {
            if (idx < 0 || idx >= ClosureContext.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(idx));
            }

            ClosureContext.GetSlot(idx).Value = value;
        }

        /// <summary>
        /// Gets the mutable cell backing an upvalue, so that host code can rebind the variable
        /// itself (and every closure sharing it) rather than a copy of its value.
        /// </summary>
        /// <param name="idx">The index of the upvalue.</param>
        /// <returns>The captured <see cref="ValueSlot"/>.</returns>
        internal ValueSlot GetUpValueSlot(int idx)
        {
            return ClosureContext.GetSlot(idx);
        }

        /// <summary>
        /// Gets the type of the upvalues contained in this closure.
        /// </summary>
        public UpValuesType CapturedUpValuesType
        {
            get
            {
                int count = UpValuesCount;

                if (count == 0)
                {
                    return default;
                }
                else if (count == 1 && GetUpValueName(0) == WellKnownSymbols.ENV)
                {
                    return UpValuesType.Environment;
                }
                else
                {
                    return UpValuesType.Closure;
                }
            }
        }

        /// <summary>
        /// Records allocation with the owning script's tracker if memory tracking is enabled.
        /// </summary>
        private static void TrackAllocation(Script script, int upValueCount)
        {
            AllocationTracker tracker = script?.AllocationTracker;
            if (tracker != null)
            {
                long totalBytes = BaseClosureOverhead + (upValueCount * PerUpValueOverhead);
                tracker.RecordAllocation(totalBytes);
            }
        }
    }
}
