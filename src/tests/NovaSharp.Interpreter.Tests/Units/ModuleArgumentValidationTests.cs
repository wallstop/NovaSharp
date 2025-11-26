namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ModuleArgumentValidationTests
    {
        [Test]
        public void RequireExecutionContextThrowsWhenContextIsNull()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
                ModuleArgumentValidation.RequireExecutionContext(null, "executionContext")
            );

            Assert.That(ex.ParamName, Is.EqualTo("executionContext"));
        }

        [Test]
        public void RequireExecutionContextReturnsProvidedContext()
        {
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());

            ScriptExecutionContext validated = ModuleArgumentValidation.RequireExecutionContext(
                context,
                "executionContext"
            );

            Assert.That(validated, Is.SameAs(context));
        }

        [Test]
        public void RequireArgumentsThrowsWhenArgsAreNull()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
                ModuleArgumentValidation.RequireArguments(null, "args")
            );

            Assert.That(ex.ParamName, Is.EqualTo("args"));
        }

        [Test]
        public void RequireArgumentsReturnsProvidedArguments()
        {
            CallbackArguments arguments = new(
                new List<DynValue>() { DynValue.NewNumber(42) },
                isMethodCall: false
            );

            CallbackArguments validated = ModuleArgumentValidation.RequireArguments(
                arguments,
                "args"
            );

            Assert.That(validated, Is.SameAs(arguments));
        }

        [Test]
        public void RequireTableThrowsWhenTableIsNull()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
                ModuleArgumentValidation.RequireTable(null, "table")
            );

            Assert.That(ex.ParamName, Is.EqualTo("table"));
        }

        [Test]
        public void RequireTableReturnsProvidedTable()
        {
            Table table = new(new Script());

            Table validated = ModuleArgumentValidation.RequireTable(table, "table");

            Assert.That(validated, Is.SameAs(table));
        }

        [Test]
        public void RequireScriptThrowsWhenScriptIsNull()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
                ModuleArgumentValidation.RequireScript(null, "script")
            );

            Assert.That(ex.ParamName, Is.EqualTo("script"));
        }

        [Test]
        public void RequireScriptReturnsProvidedScript()
        {
            Script script = new();

            Script validated = ModuleArgumentValidation.RequireScript(script, "script");

            Assert.That(validated, Is.SameAs(script));
        }
    }
}
