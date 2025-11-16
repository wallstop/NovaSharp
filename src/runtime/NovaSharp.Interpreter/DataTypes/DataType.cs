namespace NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Enumeration of possible data types in NovaSharp
    /// </summary>
    [SuppressMessage(
        "Naming",
        "CA1720:Identifier contains type name",
        Justification = "Lua type names intentionally align with string semantics."
    )]
    public enum DataType
    {
        // DO NOT MODIFY ORDER - IT'S SIGNIFICANT

        /// <summary>
        /// A nil value, as in Lua
        /// </summary>
        Nil = 0,

        /// <summary>
        /// A place holder for no value
        /// </summary>
        Void = 1,

        /// <summary>
        /// A Lua boolean
        /// </summary>
        Boolean = 2,

        /// <summary>
        /// A Lua number
        /// </summary>
        Number = 3,

        /// <summary>
        /// A Lua string
        /// </summary>
        String = 4,

        /// <summary>
        /// A Lua function
        /// </summary>
        Function = 5,

        /// <summary>
        /// A Lua table
        /// </summary>
        Table = 6,

        /// <summary>
        /// A set of multiple values
        /// </summary>
        Tuple = 7,

        /// <summary>
        /// A userdata reference - that is a wrapped CLR object
        /// </summary>
        UserData = 8,

        /// <summary>
        /// A coroutine handle
        /// </summary>
        Thread = 9,

        /// <summary>
        /// A callback function
        /// </summary>
        ClrFunction = 10,

        /// <summary>
        /// A request to execute a tail call
        /// </summary>
        TailCallRequest = 11,

        /// <summary>
        /// A request to coroutine.yield
        /// </summary>
        YieldRequest = 12,
    }

    /// <summary>
    /// Extension methods to DataType
    /// </summary>
    public static class LuaTypeExtensions
    {
        internal const DataType MAX_META_TYPES = DataType.Table;
        internal const DataType MAX_CONVERTIBLE_TYPES = DataType.ClrFunction;

        /// <summary>
        /// Determines whether this data type can have type metatables.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static bool CanHaveTypeMetatables(this DataType type)
        {
            return (int)type < (int)MAX_META_TYPES;
        }

        /// <summary>
        /// Converts the DataType to the string returned by the "type(...)" Lua function
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        /// <exception cref="ScriptRuntimeException">The DataType is not a Lua type</exception>
        public static string ToErrorTypeString(this DataType type)
        {
            switch (type)
            {
                case DataType.Void:
                    return "no value";
                case DataType.Nil:
                    return "nil";
                case DataType.Boolean:
                    return "boolean";
                case DataType.Number:
                    return "number";
                case DataType.String:
                    return "string";
                case DataType.Function:
                    return "function";
                case DataType.ClrFunction:
                    return "function";
                case DataType.Table:
                    return "table";
                case DataType.UserData:
                    return "userdata";
                case DataType.Thread:
                    return "coroutine";
                case DataType.Tuple:
                case DataType.TailCallRequest:
                case DataType.YieldRequest:
                default:
                    return $"internal<{type.ToLuaDebuggerString()}>";
            }
        }

        /// <summary>
        /// Converts the DataType to the string returned by the "type(...)" Lua function, with additional values
        /// to support debuggers
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        /// <exception cref="ScriptRuntimeException">The DataType is not a Lua type</exception>
        public static string ToLuaDebuggerString(this DataType type)
        {
            return type.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Converts the DataType to the string returned by the "type(...)" Lua function
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        /// <exception cref="ScriptRuntimeException">The DataType is not a Lua type</exception>
        public static string ToLuaTypeString(this DataType type)
        {
            switch (type)
            {
                case DataType.Void:
                case DataType.Nil:
                    return "nil";
                case DataType.Boolean:
                    return "boolean";
                case DataType.Number:
                    return "number";
                case DataType.String:
                    return "string";
                case DataType.Function:
                    return "function";
                case DataType.ClrFunction:
                    return "function";
                case DataType.Table:
                    return "table";
                case DataType.UserData:
                    return "userdata";
                case DataType.Thread:
                    return "thread";
                case DataType.Tuple:
                case DataType.TailCallRequest:
                case DataType.YieldRequest:
                default:
                    throw new ScriptRuntimeException("Unexpected LuaType {0}", type);
            }
        }
    }
}
