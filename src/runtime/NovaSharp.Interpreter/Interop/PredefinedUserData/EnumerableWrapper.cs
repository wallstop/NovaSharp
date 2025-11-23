namespace NovaSharp.Interpreter.Interop.PredefinedUserData
{
    using System.Collections;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Converters;

    /// <summary>
    /// Wrappers for enumerables as return types
    /// </summary>
    internal class EnumerableWrapper : IUserDataType
    {
        private readonly IEnumerator _enumerator;
        private readonly Script _script;
        private DynValue _prev = DynValue.Nil;
        private bool _hasTurnOnce;

        private EnumerableWrapper(Script script, IEnumerator enumerator)
        {
            _script = script;
            _enumerator = enumerator;
        }

        /// <summary>
        /// Resets the wrapped enumerator so subsequent iterations restart from the beginning.
        /// </summary>
        public void Reset()
        {
            if (_hasTurnOnce)
            {
                _enumerator.Reset();
            }

            _hasTurnOnce = true;
        }

        /// <summary>
        /// Advances the enumerator and returns the next script-friendly value.
        /// </summary>
        private DynValue GetNext(DynValue prev)
        {
            if (prev.IsNil())
            {
                Reset();
            }

            while (_enumerator.MoveNext())
            {
                DynValue v = ClrToScriptConversions.ObjectToDynValue(_script, _enumerator.Current);

                if (!v.IsNil())
                {
                    return v;
                }
            }

            return DynValue.Nil;
        }

        /// <summary>
        /// Callback that exposes the enumerator as a Lua iterator triple.
        /// </summary>
        private DynValue LuaIteratorCallback(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            _prev = GetNext(_prev);
            return _prev;
        }

        /// <summary>
        /// Wraps the provided <see cref="IEnumerator"/> so Lua code can iterate over it.
        /// </summary>
        internal static DynValue ConvertIterator(Script script, IEnumerator enumerator)
        {
            EnumerableWrapper ei = new(script, enumerator);
            return DynValue.NewTuple(UserData.Create(ei), DynValue.Nil, DynValue.Nil);
        }

        /// <summary>
        /// Exposes the values of a Lua table as a CLR-style iterator triple.
        /// </summary>
        internal static DynValue ConvertTable(Table table)
        {
            return ConvertIterator(table.OwnerScript, table.Values.GetEnumerator());
        }

        /// <summary>
        /// Implements member access on the iterator wrapper (e.g., Current/MoveNext/Reset).
        /// </summary>
        public DynValue Index(Script script, DynValue index, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                string idx = index.String;

                if (idx == "Current" || idx == "current")
                {
                    return DynValue.FromObject(script, _enumerator.Current);
                }
                else if (idx == "MoveNext" || idx == "moveNext" || idx == "move_next")
                {
                    return DynValue.NewCallback(
                        (ctx, args) => DynValue.NewBoolean(_enumerator.MoveNext())
                    );
                }
                else if (idx == "Reset" || idx == "reset")
                {
                    return DynValue.NewCallback(
                        (ctx, args) =>
                        {
                            Reset();
                            return DynValue.Nil;
                        }
                    );
                }
            }
            return null;
        }

        /// <summary>
        /// Iterator wrapper is read-only; assignments are ignored.
        /// </summary>
        public bool SetIndex(Script script, DynValue index, DynValue value, bool isDirectIndexing)
        {
            return false;
        }

        /// <summary>
        /// Provides metamethods required to drive the iterator from Lua (<c>__call</c>).
        /// </summary>
        public DynValue MetaIndex(Script script, string metaname)
        {
            if (metaname == "__call")
            {
                return DynValue.NewCallback(LuaIteratorCallback);
            }
            else
            {
                return null;
            }
        }
    }
}
