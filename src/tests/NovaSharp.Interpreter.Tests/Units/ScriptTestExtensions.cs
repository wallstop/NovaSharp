namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using Execution;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    internal static class ScriptTestExtensions
    {
        public static DynValue Evaluate(this Script script, string expression)
        {
            ArgumentNullException.ThrowIfNull(script);
            ArgumentNullException.ThrowIfNull(expression);

            DynamicExpression dynamic = script.CreateDynamicExpression(expression.Trim());
            return dynamic.Evaluate();
        }
    }
}
