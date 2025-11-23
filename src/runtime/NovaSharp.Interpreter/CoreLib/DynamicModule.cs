namespace NovaSharp.Interpreter.CoreLib
{
    using System.Diagnostics.CodeAnalysis;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;

    [SuppressMessage(
        "Design",
        "CA1052:Static holder types should be static or not inheritable",
        Justification = "Module types participate in generic registration requiring instance types."
    )]
    [NovaSharpModule(Namespace = "dynamic")]
    /// <summary>
    /// Implements NovaSharp's `dynamic` module, enabling scripts to compile and execute Lua code
    /// strings at runtime (similar to <c>load</c> but optimized for short expressions).
    /// </summary>
    public class DynamicModule
    {
        private class DynamicExpressionWrapper
        {
            public DynamicExpression Expression;
        }

        /// <summary>
        /// Registers the dynamic expression userdata wrapper so prepared expressions can be cached
        /// and executed later.
        /// </summary>
        /// <param name="globalTable">Global table passed by the module bootstrapper.</param>
        /// <param name="stringTable">Shared string table (unused, but part of the module signature).</param>
        public static void NovaSharpInit(Table globalTable, Table stringTable)
        {
            UserData.RegisterType<DynamicExpressionWrapper>(InteropAccessMode.HideMembers);
        }

        /// <summary>
        /// Compiles and executes a Lua expression provided as a string, or executes a previously
        /// prepared dynamic expression userdata.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">
        /// Callback arguments containing either a string chunk or a prepared expression userdata.
        /// </param>
        /// <returns>The evaluated result as a <see cref="DynValue"/>.</returns>
        /// <exception cref="ScriptRuntimeException">
        /// Thrown when the input cannot be parsed or is not a prepared expression userdata.
        /// </exception>
        [NovaSharpModuleMethod(Name = "eval")]
        public static DynValue Eval(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            try
            {
                if (args[0].Type == DataType.UserData)
                {
                    UserData ud = args[0].UserData;
                    if (ud.Object is DynamicExpressionWrapper wrapper)
                    {
                        return wrapper.Expression.Evaluate(executionContext);
                    }
                    else
                    {
                        throw ScriptRuntimeException.BadArgument(
                            0,
                            "dynamic.eval",
                            "A userdata was passed, but was not a previously prepared expression."
                        );
                    }
                }
                else
                {
                    DynValue vs = args.AsType(0, "dynamic.eval", DataType.String, false);
                    DynamicExpression expression = executionContext
                        .GetScript()
                        .CreateDynamicExpression(vs.String);
                    return expression.Evaluate(executionContext);
                }
            }
            catch (SyntaxErrorException ex)
            {
                throw new ScriptRuntimeException(ex);
            }
        }

        /// <summary>
        /// Compiles a Lua expression string and returns a userdata wrapper that can be evaluated
        /// multiple times without recompilation via <c>dynamic.eval</c>.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the Lua expression string to compile.</param>
        /// <returns>
        /// A userdata encapsulating the compiled <see cref="DynamicExpression"/> for later execution.
        /// </returns>
        /// <exception cref="ScriptRuntimeException">
        /// Thrown when the supplied expression fails to parse.
        /// </exception>
        [NovaSharpModuleMethod(Name = "prepare")]
        public static DynValue Prepare(
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
                DynValue vs = args.AsType(0, "dynamic.prepare", DataType.String, false);
                DynamicExpression expression = executionContext
                    .GetScript()
                    .CreateDynamicExpression(vs.String);
                return UserData.Create(new DynamicExpressionWrapper() { Expression = expression });
            }
            catch (SyntaxErrorException ex)
            {
                throw new ScriptRuntimeException(ex);
            }
        }
    }
}
