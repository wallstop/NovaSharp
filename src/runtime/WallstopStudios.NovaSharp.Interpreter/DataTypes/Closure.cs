namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.Scopes;

    /// <summary>
    /// A class representing a script function
    /// </summary>
    public class Closure : RefIdObject, IScriptPrivateResource
    {
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
        /// Gets or sets a cached <see cref="DynValue"/> wrapping this closure.
        /// Used by <see cref="DynValue.FromClosure"/> to avoid repeated allocations.
        /// </summary>
        internal DynValue CachedDynValue { get; set; }

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
        /// Initializes a new instance of the <see cref="Closure"/> class from a list of resolved locals.
        /// This overload avoids enumerator allocation by using the list directly.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="idx">The bytecode entry point index.</param>
        /// <param name="symbols">The symbol references for upvalues.</param>
        /// <param name="resolvedLocals">The resolved local values.</param>
        internal Closure(Script script, int idx, SymbolRef[] symbols, List<DynValue> resolvedLocals)
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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Closure"/> class from an array of resolved locals.
        /// This overload avoids enumerator allocation entirely.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="idx">The bytecode entry point index.</param>
        /// <param name="symbols">The symbol references for upvalues.</param>
        /// <param name="resolvedLocals">The resolved local values.</param>
        internal Closure(Script script, int idx, SymbolRef[] symbols, DynValue[] resolvedLocals)
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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Closure"/> class.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="idx">The index.</param>
        /// <param name="symbols">The symbols.</param>
        /// <param name="resolvedLocals">The resolved locals.</param>
        internal Closure(
            Script script,
            int idx,
            SymbolRef[] symbols,
            IEnumerable<DynValue> resolvedLocals
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
        }

        /// <summary>
        /// Calls this function with the specified args
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call()
        {
            return OwnerScript.Call(this);
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
            return OwnerScript.Call(this, args);
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
        /// Gets the value of an upvalue. To set the value, use GetUpValue(idx).Assign(...);
        /// </summary>
        /// <param name="idx">The index of the upvalue.</param>
        /// <returns>The value of an upvalue </returns>
        public DynValue GetUpValue(int idx)
        {
            return ClosureContext[idx];
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
    }
}
