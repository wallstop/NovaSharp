namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Compatibility
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

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
                await Assert
                    .That(type.CanHaveTypeMetatables())
                    .IsEqualTo(expected)
                    .ConfigureAwait(false);
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
                await Assert.That(type.ToLuaTypeString()).IsEqualTo(expected).ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ToLuaTypeStringThrowsForInternalTypes()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                DataType.Tuple.ToLuaTypeString()
            );

            await Assert
                .That(exception.Message)
                .Contains("Unexpected LuaType")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToErrorTypeStringProvidesDebuggerFallback()
        {
            await Assert
                .That(DataType.Void.ToErrorTypeString())
                .IsEqualTo("no value")
                .ConfigureAwait(false);
            await Assert
                .That(DataType.ClrFunction.ToErrorTypeString())
                .IsEqualTo("function")
                .ConfigureAwait(false);
            await Assert
                .That(DataType.Tuple.ToErrorTypeString())
                .IsEqualTo("internal<tuple>")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToLuaDebuggerStringLowerCasesEnumNames()
        {
            await Assert
                .That(DataType.TailCallRequest.ToLuaDebuggerString())
                .IsEqualTo("tailcallrequest")
                .ConfigureAwait(false);
        }
    }
}
