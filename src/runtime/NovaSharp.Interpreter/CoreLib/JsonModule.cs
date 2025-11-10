namespace NovaSharp.Interpreter.CoreLib
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;
    using Serialization.Json;

    [NovaSharpModule(Namespace = "json")]
    public class JsonModule
    {
        [NovaSharpModuleMethod(Name = "parse")]
        public static DynValue Parse(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            try
            {
                DynValue vs = args.AsType(0, "parse", DataType.String, false);
                Table t = JsonTableConverter.JsonToTable(vs.String, executionContext.GetScript());
                return DynValue.NewTable(t);
            }
            catch (SyntaxErrorException ex)
            {
                throw new ScriptRuntimeException(ex);
            }
        }

        [NovaSharpModuleMethod(Name = "serialize")]
        public static DynValue Serialize(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
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

        [NovaSharpModuleMethod(Name = "isnull")]
        public static DynValue Isnull(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue vs = args[0];
            return DynValue.NewBoolean((JsonNull.IsJsonNull(vs)) || (vs.IsNil()));
        }

        [NovaSharpModuleMethod(Name = "null")]
        public static DynValue Null(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return JsonNull.Create();
        }
    }
}
