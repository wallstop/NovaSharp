using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NovaSharp.Interpreter.Serialization.Json;

namespace NovaSharp.Interpreter.CoreLib
{
    [NovaSharpModule(Namespace = "json")]
    public class JsonModule
    {
        [NovaSharpModuleMethod]
        public static DynValue parse(
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

        [NovaSharpModuleMethod]
        public static DynValue serialize(
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

        [NovaSharpModuleMethod]
        public static DynValue isnull(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue vs = args[0];
            return DynValue.NewBoolean((JsonNull.IsJsonNull(vs)) || (vs.IsNil()));
        }

        [NovaSharpModuleMethod]
        public static DynValue @null(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return JsonNull.Create();
        }
    }
}
