#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

    public sealed class LuaTypeExtensionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CanHaveTypeMetatablesHonorsThreshold()
        {
            IReadOnlyList<(DataType Type, bool Expected)> cases = new (DataType, bool)[]
            {
                (DataType.Nil, true),
                (DataType.Number, true),
                (DataType.Table, false),
                (DataType.UserData, false),
            };

            foreach ((DataType type, bool expected) in cases)
            {
                await Assert.That(type.CanHaveTypeMetatables()).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ToLuaTypeStringMapsLuaVisibleTypes()
        {
            IReadOnlyList<(DataType Type, string Expected)> cases = new (DataType, string)[]
            {
                (DataType.Void, "nil"),
                (DataType.Nil, "nil"),
                (DataType.Boolean, "boolean"),
                (DataType.Number, "number"),
                (DataType.String, "string"),
                (DataType.Function, "function"),
                (DataType.ClrFunction, "function"),
                (DataType.Table, "table"),
                (DataType.UserData, "userdata"),
                (DataType.Thread, "thread"),
            };

            foreach ((DataType type, string expected) in cases)
            {
                await Assert.That(type.ToLuaTypeString()).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ToLuaTypeStringThrowsForInternalTypes()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                DataType.Tuple.ToLuaTypeString()
            );

            await Assert.That(exception.Message).Contains("Unexpected LuaType");
        }

        [global::TUnit.Core.Test]
        public async Task ToErrorTypeStringProvidesDebuggerFallback()
        {
            await Assert.That(DataType.Void.ToErrorTypeString()).IsEqualTo("no value");
            await Assert.That(DataType.ClrFunction.ToErrorTypeString()).IsEqualTo("function");
            await Assert.That(DataType.Tuple.ToErrorTypeString()).IsEqualTo("internal<tuple>");
        }

        [global::TUnit.Core.Test]
        public async Task ToLuaDebuggerStringLowerCasesEnumNames()
        {
            await Assert
                .That(DataType.TailCallRequest.ToLuaDebuggerString())
                .IsEqualTo("tailcallrequest");
        }
    }
}
#pragma warning restore CA2007
