namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class NumericForLoopEntryTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void FloatingLoopEntryUsesReferenceControlValues(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaFixtureHelper fixture =
                LuaFixtureHelper.ForCallingTest<NumericForLoopEntryTUnitTests>();
            script.DoFile(fixture.GetFixturePath());
        }
    }
}
