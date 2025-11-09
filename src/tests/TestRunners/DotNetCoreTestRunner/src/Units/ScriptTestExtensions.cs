namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using Interpreter;

    internal static class ScriptTestExtensions
    {
        public static DynValue Evaluate(this Script script, string expression)
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            DynamicExpression dynamic = script.CreateDynamicExpression(expression.Trim());
            return dynamic.Evaluate();
        }
    }
}
