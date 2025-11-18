// Disable warnings about XML documentation
#pragma warning disable 1591

namespace NovaSharp.Interpreter.CoreLib
{
    using System.Diagnostics.CodeAnalysis;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing dynamic expression evaluations at runtime (a NovaSharp addition).
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1052:Static holder types should be static or not inheritable",
        Justification = "Module types participate in generic registration requiring instance types."
    )]
    [NovaSharpModule(Namespace = "dynamic")]
    public class DynamicModule
    {
        private class DynamicExpressionWrapper
        {
            public DynamicExpression Expression;
        }

        public static void NovaSharpInit(Table globalTable, Table stringTable)
        {
            UserData.RegisterType<DynamicExpressionWrapper>(InteropAccessMode.HideMembers);
        }

        [NovaSharpModuleMethod(Name = "eval")]
        public static DynValue Eval(ScriptExecutionContext executionContext, CallbackArguments args)
        {
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

        [NovaSharpModuleMethod(Name = "prepare")]
        public static DynValue Prepare(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
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
