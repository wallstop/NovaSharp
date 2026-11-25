namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class FunctionTypeMetatableTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FunctionKindsShareMetatableAndDispatch(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoFile(
                LuaFixtureHelper.ForCallingTest<FunctionTypeMetatableTUnitTests>().GetFixturePath()
            );

            Table first = new(script);
            script.SetTypeMetatable(DataType.ClrFunction, first);
            await Assert
                .That(script.GetTypeMetatable(DataType.Function))
                .IsSameReferenceAs(first)
                .ConfigureAwait(false);

            Script other = new(version, CoreModulePresets.Complete);
            await Assert
                .That(other.GetTypeMetatable(DataType.ClrFunction))
                .IsNull()
                .ConfigureAwait(false);
            Table second = new(other);
            other.SetTypeMetatable(DataType.Function, second);
            await Assert
                .That(other.GetTypeMetatable(DataType.ClrFunction))
                .IsSameReferenceAs(second)
                .ConfigureAwait(false);
            await Assert
                .That(script.GetTypeMetatable(DataType.ClrFunction))
                .IsSameReferenceAs(first)
                .ConfigureAwait(false);

            script.SetTypeMetatable(DataType.Function, null);
            await Assert
                .That(script.GetTypeMetatable(DataType.ClrFunction))
                .IsNull()
                .ConfigureAwait(false);
        }
    }
}
