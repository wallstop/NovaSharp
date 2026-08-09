namespace WallstopStudios.NovaSharp.Interpreter.Interop.PredefinedUserData
{
    using System.Collections;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Converters;

    /// <summary>
    /// Wrappers for enumerables as return types
    /// </summary>
    internal class EnumerableWrapper : IUserDataTypeTryAccess
    {
        private readonly IEnumerator _enumerator;
        private readonly Script _script;
        private LuaValue _prev = LuaValue.Nil;
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
        private LuaValue GetNext(LuaValue prev)
        {
            if (prev.IsNil)
            {
                Reset();
            }

            while (_enumerator.MoveNext())
            {
                LuaValue v = ClrToScriptConversions.ObjectToDynValue(_script, _enumerator.Current);

                if (!v.IsNil)
                {
                    return v;
                }
            }

            return LuaValue.Nil;
        }

        /// <summary>
        /// Callback that exposes the enumerator as a Lua iterator triple.
        /// </summary>
        private LuaValue LuaIteratorCallback(
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
        internal static LuaValue ConvertIterator(Script script, IEnumerator enumerator)
        {
            EnumerableWrapper ei = new(script, enumerator);
            UserData.TryCreate(script, ei, out LuaValue iterator);
            return LuaValue.NewTuple(iterator, LuaValue.Nil, LuaValue.Nil);
        }

        /// <summary>
        /// Exposes the values of a Lua table as a CLR-style iterator triple.
        /// </summary>
        internal static LuaValue ConvertTable(Table table)
        {
            return ConvertIterator(table.OwnerScript, table.Values.GetEnumerator());
        }

        /// <summary>
        /// Implements member access on the iterator wrapper (e.g., Current/MoveNext/Reset).
        /// </summary>
        public LuaValue? Index(Script script, LuaValue index, bool isDirectIndexing)
        {
            return TryIndex(script, index, isDirectIndexing, out LuaValue value)
                ? value
                : (LuaValue?)null;
        }

        /// <inheritdoc/>
        public bool TryIndex(
            Script script,
            LuaValue index,
            bool isDirectIndexing,
            out LuaValue value
        )
        {
            if (index.Type == DataType.String)
            {
                string idx = index.String;

                if (idx == "Current" || idx == "current")
                {
                    value = LuaValue.FromObject(script, _enumerator.Current);
                    return true;
                }
                else if (idx == "MoveNext" || idx == "moveNext" || idx == "move_next")
                {
                    value = LuaValue.NewCallback(
                        script,
                        (ctx, args) => LuaValue.NewBoolean(_enumerator.MoveNext())
                    );
                    return true;
                }
                else if (idx == "Reset" || idx == "reset")
                {
                    value = LuaValue.NewCallback(
                        script,
                        (ctx, args) =>
                        {
                            Reset();
                            return LuaValue.Nil;
                        }
                    );
                    return true;
                }
            }

            value = LuaValue.Nil;
            return false;
        }

        /// <summary>
        /// Iterator wrapper is read-only; assignments are ignored.
        /// </summary>
        public bool SetIndex(Script script, LuaValue index, LuaValue value, bool isDirectIndexing)
        {
            return false;
        }

        /// <summary>
        /// Provides metamethods required to drive the iterator from Lua (<c>__call</c>).
        /// </summary>
        public LuaValue? MetaIndex(Script script, string metaname)
        {
            return TryMetaIndex(script, metaname, out LuaValue value) ? value : (LuaValue?)null;
        }

        /// <inheritdoc/>
        public bool TryMetaIndex(Script script, string metaname, out LuaValue value)
        {
            if (metaname == Metamethods.Call)
            {
                value = LuaValue.NewCallback(script, LuaIteratorCallback);
                return true;
            }

            value = LuaValue.Nil;
            return false;
        }
    }
}
