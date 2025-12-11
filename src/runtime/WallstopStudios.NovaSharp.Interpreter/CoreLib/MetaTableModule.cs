namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Implements Lua metatable helpers (`setmetatable`, `getmetatable`, and the raw* primitives)
    /// per Lua 5.4 §6.5.
    /// </summary>
    [NovaSharpModule]
    public static class MetaTableModule
    {
        // setmetatable (table, metatable)
        // -------------------------------------------------------------------------------------------------------------------
        // Sets the metatable for the given table. (You cannot change the metatable of other
        // types from Lua, only from C.) If metatable is nil, removes the metatable of the given table.
        // If the original metatable has a "__metatable" field, raises an error ("cannot change a protected metatable").
        // This function returns table.
        /// <summary>
        /// Implements Lua `setmetatable`, updating a table's metatable or removing it when passed
        /// <c>nil</c>. Respects the `__metatable` protection flag (§6.5).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments providing the table and new metatable.</param>
        /// <returns>The original table.</returns>
        [NovaSharpModuleMethod(Name = "setmetatable")]
        public static DynValue SetMetatable(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue table = args.AsType(0, "setmetatable", DataType.Table);
            DynValue metatable = args.AsType(1, "setmetatable", DataType.Table, true);

            DynValue curmeta = executionContext.GetMetamethod(table, "__metatable");

            if (curmeta != null)
            {
                throw new ScriptRuntimeException("cannot change a protected metatable");
            }

            table.Table.MetaTable = metatable.Table;
            return table;
        }

        // getmetatable (object)
        // -------------------------------------------------------------------------------------------------------------------
        // If object does not have a metatable, returns nil. Otherwise, if the object's metatable
        // has a "__metatable" field, returns the associated value. Otherwise, returns the metatable of the given object.
        /// <summary>
        /// Implements Lua `getmetatable`, returning the metatable or the protected `__metatable`
        /// marker when present (§6.5).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the object to inspect.</param>
        /// <returns>The metatable, `__metatable` value, or <c>nil</c>.</returns>
        [NovaSharpModuleMethod(Name = "getmetatable")]
        public static DynValue GetMetatable(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue obj = args[0];
            Table meta = null;

            if (obj.Type.CanHaveTypeMetatables())
            {
                meta = executionContext.Script.GetTypeMetatable(obj.Type);
            }

            if (obj.Type == DataType.Table)
            {
                meta = obj.Table.MetaTable;
            }

            if (meta == null)
            {
                return DynValue.Nil;
            }
            else if (meta.RawGet("__metatable") != null)
            {
                return meta.Get("__metatable");
            }
            else
            {
                return DynValue.NewTable(meta);
            }
        }

        // rawget (table, index)
        // -------------------------------------------------------------------------------------------------------------------
        // Gets the real value of table[index], without invoking any metamethod. table must be a table; index may be any value.
        /// <summary>
        /// Implements Lua `rawget`, bypassing metamethods to retrieve a table entry (§6.5).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments specifying the table and key.</param>
        /// <returns>The raw value stored at the key.</returns>
        [NovaSharpModuleMethod(Name = "rawget")]
        public static DynValue RawGet(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue table = args.AsType(0, "rawget", DataType.Table);
            DynValue index = args[1];

            return table.Table.Get(index);
        }

        // rawset (table, index, value)
        // -------------------------------------------------------------------------------------------------------------------
        // Sets the real value of table[index] to value, without invoking any metamethod. table must be a table,
        // index any value different from nil and NaN, and value any Lua value.
        // This function returns table.
        /// <summary>
        /// Implements Lua `rawset`, writing to a table without invoking metamethods (§6.5).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments providing the table, key, and value.</param>
        /// <returns>The original table.</returns>
        [NovaSharpModuleMethod(Name = "rawset")]
        public static DynValue RawSet(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue table = args.AsType(0, "rawset", DataType.Table);
            DynValue index = args[1];

            table.Table.Set(index, args[2]);

            return table;
        }

        // rawequal (v1, v2)
        // -------------------------------------------------------------------------------------------------------------------
        // Checks whether v1 is equal to v2, without invoking any metamethod. Returns a boolean.
        /// <summary>
        /// Implements Lua `rawequal`, comparing two values without metamethod dispatch (§6.5).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the two values.</param>
        /// <returns>Boolean result.</returns>
        [NovaSharpModuleMethod(Name = "rawequal")]
        public static DynValue RawEqual(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue v1 = args[0];
            DynValue v2 = args[1];

            return DynValue.FromBoolean(v1.Equals(v2));
        }

        //rawlen (v)
        // -------------------------------------------------------------------------------------------------------------------
        //Returns the length of the object v, which must be a table or a string, without invoking any metamethod. Returns an integer number.
        /// <summary>
        /// Implements Lua `rawlen`, returning the raw length of a string or table without
        /// metamethods (§6.5).
        /// </summary>
        /// <remarks>
        /// This function was added in Lua 5.2 and is not available in Lua 5.1 mode.
        /// </remarks>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the target string/table.</param>
        /// <returns>Length as a number.</returns>
        [LuaCompatibility(LuaCompatibilityVersion.Lua52)]
        [NovaSharpModuleMethod(Name = "rawlen")]
        public static DynValue RawLen(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            if (args[0].Type != DataType.String && args[0].Type != DataType.Table)
            {
                throw ScriptRuntimeException.BadArgument(
                    0,
                    "rawlen",
                    "table or string",
                    args[0].Type.ToErrorTypeString(),
                    false
                );
            }

            return args[0].GetLength();
        }
    }
}
