namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Public Lua table wrapper.
    /// </summary>
    public sealed class LuaTable
    {
        private readonly Script _script;
        private readonly Table _table;

        internal LuaTable(Script script, Table table)
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            _script = script;
            _table = table;
        }

        /// <summary>
        /// Gets or sets a value by string key.
        /// </summary>
        public LuaValue this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        /// <summary>
        /// Gets or sets a value by one-based integer key.
        /// </summary>
        public LuaValue this[int key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        /// <summary>
        /// Gets the contiguous array length.
        /// </summary>
        public int Length
        {
            get
            {
                _script.ThrowIfDisposed();
                return _table.Length;
            }
        }

        /// <summary>
        /// Gets a value by string key.
        /// </summary>
        public LuaValue Get(string key)
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.Wrap(_script, _table.Get(key));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Gets a value by one-based integer key.
        /// </summary>
        public LuaValue Get(int key)
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.Wrap(_script, _table.Get(key));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Sets a value by string key.
        /// </summary>
        public void Set(string key, LuaValue value)
        {
            _script.ThrowIfDisposed();
            try
            {
                _table.Set(key, value.ToDynValue(_script));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Sets a value by one-based integer key.
        /// </summary>
        public void Set(int key, LuaValue value)
        {
            _script.ThrowIfDisposed();
            try
            {
                _table.Set(key, value.ToDynValue(_script));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Sets or clears this table's metatable.
        /// </summary>
        /// <remarks>
        /// This host-side API bypasses Lua's <c>__metatable</c> protection and is not equivalent
        /// to Lua's <c>setmetatable</c> library function.
        /// </remarks>
        public void SetMetatable(LuaTable metatable)
        {
            _script.ThrowIfDisposed();
            if (metatable != null)
            {
                LuaEngine.EnsureSameOwner(metatable.OwnerScript, _script);
            }

            try
            {
                _table.MetaTable = metatable == null ? null : metatable.Table;
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Removes a string key from the table.
        /// </summary>
        public bool Remove(string key)
        {
            _script.ThrowIfDisposed();
            try
            {
                return _table.Remove(key);
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Removes a one-based integer key from the table.
        /// </summary>
        public bool Remove(int key)
        {
            _script.ThrowIfDisposed();
            try
            {
                return _table.Remove(key);
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Wraps this table as a Lua value for assignment or calls.
        /// </summary>
        public LuaValue ToValue()
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.Wrap(_script, LuaValue.NewTable(_table));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Gets the engine that owns this table.
        /// </summary>
        internal Script OwnerScript => _script;

        internal Table Table
        {
            get
            {
                _script.ThrowIfDisposed();
                return _table;
            }
        }
    }
}
