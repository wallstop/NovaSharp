namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using global::NovaSharp;
    using Serialization.Json;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Provides Lua-facing helpers for converting between NovaSharp tables and JSON strings, plus a
    /// canonical `json.null` representation.
    /// </summary>
    [NovaSharpModule(Namespace = "json")]
    public static class JsonModule
    {
        /// <summary>
        /// Parses a JSON string into a Lua table hierarchy using NovaSharp's JSON converter.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Callback arguments; index 0 must contain the JSON string.</param>
        /// <returns>A table representing the decoded JSON document.</returns>
        /// <exception cref="ScriptRuntimeException">
        /// Thrown when the input cannot be parsed according to JSON syntax.
        /// </exception>
        [NovaSharpModuleMethod(Name = "parse")]
        public static LuaValue Parse(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            try
            {
                LuaValue vs = args.AsType(executionContext, 0, "parse", DataType.String, false);
                Table t = JsonTableConverter.JsonToTable(vs.String, executionContext.Script);
                return LuaValue.NewTable(t);
            }
            catch (SyntaxErrorException ex)
            {
                throw new ScriptRuntimeException(ex);
            }
        }

        /// <summary>
        /// Converts a Lua table into its JSON string representation.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments where index 0 must be the table to serialize.</param>
        /// <returns>String LuaValue containing the JSON payload.</returns>
        /// <exception cref="ScriptRuntimeException">
        /// Thrown when the table structure cannot be serialized to JSON.
        /// </exception>
        [NovaSharpModuleMethod(Name = "serialize")]
        public static LuaValue Serialize(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            try
            {
                LuaValue vt = args.AsType(0, "serialize", DataType.Table, false);
                string s = JsonTableConverter.TableToJson(vt.Table);
                return LuaValue.NewString(s);
            }
            catch (SyntaxErrorException ex)
            {
                throw new ScriptRuntimeException(ex);
            }
        }

        /// <summary>
        /// Checks whether the supplied value represents `json.null` (or plain Lua nil) for easier
        /// comparisons in Lua scripts.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">
        /// Callback arguments where index 0 contains the value being tested.
        /// </param>
        /// <returns>
        /// Boolean LuaValue indicating <see langword="true"/> when the argument equals `json.null`
        /// or Lua <c>nil</c>.
        /// </returns>
        [NovaSharpModuleMethod(Name = "isnull")]
        public static LuaValue IsNull(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue vs = args[0];
            return LuaValue.FromBoolean((JsonNull.IsJsonNull(vs)) || (vs.IsNil));
        }

        /// <summary>
        /// Returns the canonical `json.null` userdata that round-trips through the serializer and
        /// parser.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Unused arguments (kept for module signature consistency).</param>
        /// <returns>The shared `json.null` LuaValue.</returns>
        [NovaSharpModuleMethod(Name = "null")]
        public static LuaValue Null(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return JsonNull.Create();
        }
    }
}
