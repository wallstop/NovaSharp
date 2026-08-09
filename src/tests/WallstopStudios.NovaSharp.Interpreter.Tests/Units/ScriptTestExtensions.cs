namespace WallstopStudios.NovaSharp.Interpreter.Tests.Units
{
    using System;
    using global::NovaSharp;
    using Execution;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    internal static class ScriptTestExtensions
    {
        public static LuaValue Evaluate(this Script script, string expression)
        {
            ArgumentNullException.ThrowIfNull(script);
            ArgumentNullException.ThrowIfNull(expression);

            DynamicExpression dynamic = script.CreateDynamicExpression(expression.Trim());
            return dynamic.Evaluate();
        }
    }
}
