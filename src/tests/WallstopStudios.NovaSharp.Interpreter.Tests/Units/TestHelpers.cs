namespace WallstopStudios.NovaSharp.Interpreter.Tests.Units
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;

    internal static class TestHelpers
    {
        public static ScriptExecutionContext CreateExecutionContext(Script script)
        {
            ArgumentNullException.ThrowIfNull(script);

            return script.CreateDynamicExecutionContext();
        }

        public static CallbackArguments CreateArguments(params DynValue[] values)
        {
            return new CallbackArguments(values, false);
        }
    }
}
