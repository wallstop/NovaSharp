namespace NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;

    /// <summary>
    /// Shared guard helpers for module entry points so analyzer-required null checks remain concise.
    /// </summary>
    internal static class ModuleArgumentValidation
    {
        [return: NotNull]
        public static ScriptExecutionContext RequireExecutionContext(
            [NotNull] ScriptExecutionContext context,
            string parameterName
        )
        {
            if (context == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return context;
        }

        [return: NotNull]
        public static CallbackArguments RequireArguments(
            [NotNull] CallbackArguments args,
            string parameterName
        )
        {
            if (args == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return args;
        }

        [return: NotNull]
        public static Table RequireTable([NotNull] Table table, string parameterName)
        {
            if (table == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return table;
        }

        [return: NotNull]
        public static Script RequireScript([NotNull] Script script, string parameterName)
        {
            if (script == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return script;
        }
    }
}
