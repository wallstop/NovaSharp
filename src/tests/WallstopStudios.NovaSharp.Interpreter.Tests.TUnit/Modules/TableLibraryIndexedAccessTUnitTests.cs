namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class TableLibraryIndexedAccessTUnitTests
    {
        [Test]
        [AllLuaVersions]
        public void OperationsUseVersionedIndexingAndLength(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper
                .ForCallingTest<TableLibraryIndexedAccessTUnitTests>()
                .RunFixture(script);
        }

        [Test]
        [AllLuaVersions]
        public void OperationsValidateOnlyRequiredProxyHooks(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper
                .ForCallingTest<TableLibraryIndexedAccessTUnitTests>()
                .RunFixture(script);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public void IndexedOperationsPreserveWidthAndFailureOrder(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper
                .ForCallingTest<TableLibraryIndexedAccessTUnitTests>()
                .RunFixture(script);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public void UnpackChecksAvailableStackBeforeIndexing(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                MaxVmValueStackSize = 64,
                ScriptLoader = new FileSystemScriptLoader(),
            };
            Script script = new(CoreModulePresets.Complete, options);
            LuaFixtureHelper
                .ForCallingTest<TableLibraryIndexedAccessTUnitTests>()
                .RunFixture(script);
        }
    }
}
