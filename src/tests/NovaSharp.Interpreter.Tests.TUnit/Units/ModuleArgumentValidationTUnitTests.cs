#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tests.Units;

    public sealed class ModuleArgumentValidationTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task RequireExecutionContextThrowsWhenContextIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                ModuleArgumentValidation.RequireExecutionContext(null, "executionContext")
            );

            await Assert.That(exception.ParamName).IsEqualTo("executionContext");
        }

        [global::TUnit.Core.Test]
        public async Task RequireExecutionContextReturnsProvidedContext()
        {
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            ScriptExecutionContext validated = ModuleArgumentValidation.RequireExecutionContext(
                context,
                "executionContext"
            );

            await Assert.That(validated).IsSameReferenceAs(context);
        }

        [global::TUnit.Core.Test]
        public async Task RequireArgumentsThrowsWhenArgsAreNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                ModuleArgumentValidation.RequireArguments(null, "args")
            );

            await Assert.That(exception.ParamName).IsEqualTo("args");
        }

        [global::TUnit.Core.Test]
        public async Task RequireArgumentsReturnsProvidedArguments()
        {
            CallbackArguments arguments = new(
                new List<DynValue> { DynValue.NewNumber(42) },
                isMethodCall: false
            );

            CallbackArguments validated = ModuleArgumentValidation.RequireArguments(
                arguments,
                "args"
            );

            await Assert.That(validated).IsSameReferenceAs(arguments);
        }

        [global::TUnit.Core.Test]
        public async Task RequireTableThrowsWhenTableIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                ModuleArgumentValidation.RequireTable(null, "table")
            );

            await Assert.That(exception.ParamName).IsEqualTo("table");
        }

        [global::TUnit.Core.Test]
        public async Task RequireTableReturnsProvidedTable()
        {
            Table table = new(new Script());

            Table validated = ModuleArgumentValidation.RequireTable(table, "table");

            await Assert.That(validated).IsSameReferenceAs(table);
        }

        [global::TUnit.Core.Test]
        public async Task RequireScriptThrowsWhenScriptIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                ModuleArgumentValidation.RequireScript(null, "script")
            );

            await Assert.That(exception.ParamName).IsEqualTo("script");
        }

        [global::TUnit.Core.Test]
        public async Task RequireScriptReturnsProvidedScript()
        {
            Script script = new();

            Script validated = ModuleArgumentValidation.RequireScript(script, "script");

            await Assert.That(validated).IsSameReferenceAs(script);
        }
    }
}
#pragma warning restore CA2007
