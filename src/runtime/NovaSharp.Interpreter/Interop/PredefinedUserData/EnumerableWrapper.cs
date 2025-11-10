namespace NovaSharp.Interpreter.Interop
{
    using System.Collections;
    using Converters;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Wrappers for enumerables as return types
    /// </summary>
    internal class EnumerableWrapper : IUserDataType
    {
        private readonly IEnumerator _enumerator;
        private readonly Script _script;
        private DynValue _prev = DynValue.Nil;
        private bool _hasTurnOnce = false;

        private EnumerableWrapper(Script script, IEnumerator enumerator)
        {
            _script = script;
            _enumerator = enumerator;
        }

        public void Reset()
        {
            if (_hasTurnOnce)
            {
                _enumerator.Reset();
            }

            _hasTurnOnce = true;
        }

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

        private DynValue LuaIteratorCallback(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            _prev = GetNext(_prev);
            return _prev;
        }

        internal static DynValue ConvertIterator(Script script, IEnumerator enumerator)
        {
            EnumerableWrapper ei = new(script, enumerator);
            return DynValue.NewTuple(UserData.Create(ei), DynValue.Nil, DynValue.Nil);
        }

        internal static DynValue ConvertTable(Table table)
        {
            return ConvertIterator(table.OwnerScript, table.Values.GetEnumerator());
        }

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

        public bool SetIndex(Script script, DynValue index, DynValue value, bool isDirectIndexing)
        {
            return false;
        }

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
