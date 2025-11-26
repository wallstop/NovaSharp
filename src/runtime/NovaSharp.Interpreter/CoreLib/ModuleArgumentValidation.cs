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
        /// <summary>
        /// Ensures a module method receives a non-null <see cref="ScriptExecutionContext"/> before
        /// dereferencing it, throwing <see cref="ArgumentNullException"/> when validation fails.
        /// </summary>
        /// <param name="context">Execution context supplied by the caller.</param>
        /// <param name="parameterName">Name of the parameter being validated.</param>
        /// <returns>The validated context for fluent usage.</returns>
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

        /// <summary>
        /// Validates that callback arguments are non-null before module entry points enumerate them.
        /// </summary>
        /// <param name="args">Arguments passed into the module method.</param>
        /// <param name="parameterName">Name of the parameter being validated.</param>
        /// <returns>The validated <see cref="CallbackArguments"/> instance.</returns>
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

        /// <summary>
        /// Validates that a <see cref="Table"/> reference is non-null before module initialization
        /// code mutates it.
        /// </summary>
        /// <param name="table">Table argument supplied by the host.</param>
        /// <param name="parameterName">Name of the parameter being validated.</param>
        /// <returns>The validated table.</returns>
        [return: NotNull]
        public static Table RequireTable([NotNull] Table table, string parameterName)
        {
            if (table == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return table;
        }

        /// <summary>
        /// Validates that a <see cref="Script"/> reference exists before modules call into it.
        /// </summary>
        /// <param name="script">Script instance provided by the host.</param>
        /// <param name="parameterName">Name of the parameter being validated.</param>
        /// <returns>The validated script.</returns>
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
