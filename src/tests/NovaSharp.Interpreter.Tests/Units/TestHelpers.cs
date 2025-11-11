namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;

    internal static class TestHelpers
    {
        public static ScriptExecutionContext CreateExecutionContext(Script script)
        {
            Type processorType = typeof(Script).Assembly.GetType(
                "NovaSharp.Interpreter.Execution.VM.Processor",
                true
            );

            object processor = Activator.CreateInstance(
                processorType,
                script,
                script.Globals.OwnerScriptOptions,
                true
            );

            return new ScriptExecutionContext(
                (dynamic)processor,
                null,
                new NovaSharp.Interpreter.Debugging.SourceRef(0, 0, 0, 0, 0)
            );
        }

        public static CallbackArguments CreateArguments(params DynValue[] values)
        {
            return new CallbackArguments(values, false);
        }
    }
}
