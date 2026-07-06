namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Public Lua table wrapper.
    /// </summary>
    public sealed class LuaTable
    {
        private readonly LuaEngine _owner;
        private readonly Table _table;

        internal LuaTable(LuaEngine owner, Table table)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            _owner = owner;
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
                _owner.ThrowIfDisposed();
                return _table.Length;
            }
        }

        /// <summary>
        /// Gets a value by string key.
        /// </summary>
        public LuaValue Get(string key)
        {
            _owner.ThrowIfDisposed();
            try
            {
                return _owner.Wrap(_table.Get(key));
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
            _owner.ThrowIfDisposed();
            try
            {
                return _owner.Wrap(_table.Get(key));
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
            _owner.ThrowIfDisposed();
            try
            {
                _table.Set(key, value.ToDynValue(_owner));
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
            _owner.ThrowIfDisposed();
            try
            {
                _table.Set(key, value.ToDynValue(_owner));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Sets or clears this table's metatable.
        /// </summary>
        public void SetMetatable(LuaTable metatable)
        {
            _owner.ThrowIfDisposed();
            if (metatable != null)
            {
                LuaEngine.EnsureSameOwner(metatable.Owner, _owner);
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
            _owner.ThrowIfDisposed();
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
            _owner.ThrowIfDisposed();
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
            _owner.ThrowIfDisposed();
            try
            {
                return _owner.Wrap(DynValue.NewTable(_table));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Gets the engine that owns this table.
        /// </summary>
        internal LuaEngine Owner => _owner;

        internal Table Table
        {
            get
            {
                _owner.ThrowIfDisposed();
                return _table;
            }
        }
    }
}
