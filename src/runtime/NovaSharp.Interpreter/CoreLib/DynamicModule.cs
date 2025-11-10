// Disable warnings about XML documentation
#pragma warning disable 1591

namespace NovaSharp.Interpreter.CoreLib
{
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Class implementing dynamic expression evaluations at runtime (a NovaSharp addition).
    /// </summary>
    [NovaSharpModule(Namespace = "dynamic")]
    public class DynamicModule
    {
        private class DynamicExprWrapper
        {
            public DynamicExpression expr;
        }

        public static void NovaSharpInit(Table globalTable, Table stringTable)
        {
            UserData.RegisterType<DynamicExprWrapper>(InteropAccessMode.HideMembers);
        }

        [NovaSharpModuleMethod(Name = "eval")]
        public static DynValue Eval(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            try
            {
                if (args[0].Type == DataType.UserData)
                {
                    UserData ud = args[0].UserData;
                    if (ud.Object is DynamicExprWrapper wrapper)
                    {
                        return wrapper.expr.Evaluate(executionContext);
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
                    DynamicExpression expr = executionContext
                        .GetScript()
                        .CreateDynamicExpression(vs.String);
                    return expr.Evaluate(executionContext);
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
                DynamicExpression expr = executionContext
                    .GetScript()
                    .CreateDynamicExpression(vs.String);
                return UserData.Create(new DynamicExprWrapper() { expr = expr });
            }
            catch (SyntaxErrorException ex)
            {
                throw new ScriptRuntimeException(ex);
            }
        }
    }
}
