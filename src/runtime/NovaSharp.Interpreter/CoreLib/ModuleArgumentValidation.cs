namespace NovaSharp.Interpreter.CoreLib
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;

    /// <summary>
    /// Shared guard helpers for module entry points so analyzer-required null checks remain concise.
    /// </summary>
    internal static class ModuleArgumentValidation
    {
        public static ScriptExecutionContext RequireExecutionContext(
            ScriptExecutionContext context,
            string parameterName
        )
        {
            if (context == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return context;
        }

        public static CallbackArguments RequireArguments(
            CallbackArguments args,
            string parameterName
        )
        {
            if (args == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return args;
        }

        public static Table RequireTable(Table table, string parameterName)
        {
            if (table == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return table;
        }

        public static Script RequireScript(Script script, string parameterName)
        {
            if (script == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return script;
        }
    }
}
