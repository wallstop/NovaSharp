using System;
using MoonSharp.Interpreter;

namespace MoonSharp.Interpreter.Tests.Units
{
    internal static class ScriptTestExtensions
    {
        public static DynValue Evaluate(this Script script, string expression)
        {
            if (script == null)
                throw new ArgumentNullException(nameof(script));

            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            var dynamic = script.CreateDynamicExpression(expression.Trim());
            return dynamic.Evaluate();
        }
    }
}
