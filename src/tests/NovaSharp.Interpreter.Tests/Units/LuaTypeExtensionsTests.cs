namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LuaTypeExtensionsTests
    {
        [TestCase(DataType.Nil, true)]
        [TestCase(DataType.Number, true)]
        [TestCase(DataType.Table, false)]
        [TestCase(DataType.UserData, false)]
        public void CanHaveTypeMetatablesHonorsThreshold(DataType type, bool expected)
        {
            Assert.That(type.CanHaveTypeMetatables(), Is.EqualTo(expected));
        }

        [TestCase(DataType.Void, "nil")]
        [TestCase(DataType.Nil, "nil")]
        [TestCase(DataType.Boolean, "boolean")]
        [TestCase(DataType.Number, "number")]
        [TestCase(DataType.String, "string")]
        [TestCase(DataType.Function, "function")]
        [TestCase(DataType.ClrFunction, "function")]
        [TestCase(DataType.Table, "table")]
        [TestCase(DataType.UserData, "userdata")]
        [TestCase(DataType.Thread, "thread")]
        public void ToLuaTypeStringMapsLuaVisibleTypes(DataType type, string expected)
        {
            Assert.That(type.ToLuaTypeString(), Is.EqualTo(expected));
        }

        [Test]
        public void ToLuaTypeStringThrowsForInternalTypes()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(
                () => DataType.Tuple.ToLuaTypeString()
            );

            Assert.That(exception.Message, Does.Contain("Unexpected LuaType"));
        }

        [Test]
        public void ToErrorTypeStringProvidesDebuggerFallback()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DataType.Void.ToErrorTypeString(), Is.EqualTo("no value"));
                Assert.That(DataType.ClrFunction.ToErrorTypeString(), Is.EqualTo("function"));
                Assert.That(DataType.Tuple.ToErrorTypeString(), Is.EqualTo("internal<tuple>"));
            });
        }

        [Test]
        public void ToLuaDebuggerStringLowerCasesEnumNames()
        {
            Assert.That(DataType.TailCallRequest.ToLuaDebuggerString(), Is.EqualTo("tailcallrequest"));
        }
    }
}
