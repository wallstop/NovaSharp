namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class TableInsertIndexedAccessTUnitTests
    {
        [Test]
        [AllLuaVersions]
        public void InsertUsesIndexedAccessInLua53AndLater(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper
                .ForCallingTest<TableInsertIndexedAccessTUnitTests>()
                .RunFixture(script);
        }

        [Test]
        [AllLuaVersions]
        public void InsertAcceptsOnlyEligibleNonTableProxies(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper
                .ForCallingTest<TableInsertIndexedAccessTUnitTests>()
                .RunFixture(script);
        }

        [Test]
        [AllLuaVersions]
        public void InsertStopsAfterAnIndexingError(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper
                .ForCallingTest<TableInsertIndexedAccessTUnitTests>()
                .RunFixture(script);
        }
    }
}
