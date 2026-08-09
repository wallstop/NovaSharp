namespace WallstopStudios.NovaSharp.Interpreter.Tests.Units
{
    using System;
    using global::NovaSharp;
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

        public static CallbackArguments CreateArguments(params LuaValue[] values)
        {
            return new CallbackArguments(values, false);
        }
    }
}
