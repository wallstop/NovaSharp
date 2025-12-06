namespace NovaSharp.Interpreter.CoreLib
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;
    using Serialization.Json;

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
        public static DynValue Parse(
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
                DynValue vs = args.AsType(0, "parse", DataType.String, false);
                Table t = JsonTableConverter.JsonToTable(vs.String, executionContext.Script);
                return DynValue.NewTable(t);
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
        /// <returns>String DynValue containing the JSON payload.</returns>
        /// <exception cref="ScriptRuntimeException">
        /// Thrown when the table structure cannot be serialized to JSON.
        /// </exception>
        [NovaSharpModuleMethod(Name = "serialize")]
        public static DynValue Serialize(
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
                DynValue vt = args.AsType(0, "serialize", DataType.Table, false);
                string s = JsonTableConverter.TableToJson(vt.Table);
                return DynValue.NewString(s);
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
        /// Boolean DynValue indicating <see langword="true"/> when the argument equals `json.null`
        /// or Lua <c>nil</c>.
        /// </returns>
        [NovaSharpModuleMethod(Name = "isnull")]
        public static DynValue IsNull(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vs = args[0];
            return DynValue.NewBoolean((JsonNull.IsJsonNull(vs)) || (vs.IsNil()));
        }

        /// <summary>
        /// Returns the canonical `json.null` userdata that round-trips through the serializer and
        /// parser.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Unused arguments (kept for module signature consistency).</param>
        /// <returns>The shared `json.null` DynValue.</returns>
        [NovaSharpModuleMethod(Name = "null")]
        public static DynValue Null(ScriptExecutionContext executionContext, CallbackArguments args)
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
