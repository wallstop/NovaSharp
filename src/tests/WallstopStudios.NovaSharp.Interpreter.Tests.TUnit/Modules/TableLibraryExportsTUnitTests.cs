namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class TableLibraryExportsTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void ExportsMatchReferenceBuild(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaFixtureHelper fixture =
                LuaFixtureHelper.ForCallingTest<TableLibraryExportsTUnitTests>();
            script.DoFile(fixture.GetFixturePath());
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua51)]
        public void LegacyHelpersMatchLua51(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            LuaFixtureHelper fixture =
                LuaFixtureHelper.ForCallingTest<TableLibraryExportsTUnitTests>();
            script.DoFile(fixture.GetFixturePath());
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua55)]
        public async Task CreateReturnsEmptyTablesAndValidatesHints(LuaCompatibilityVersion version)
        {
            Script script = new(
                new ScriptOptions
                {
                    CompatibilityVersion = version,
                    Sandbox = new SandboxOptions { MaxMemoryBytes = 16 * 1024 * 1024 },
                }
            );
            LuaFixtureHelper fixture =
                LuaFixtureHelper.ForCallingTest<TableLibraryExportsTUnitTests>();
            script.DoString(fixture.LoadFixtureContent());

            LuaValue create = script.Globals.Get("table").Table.Get("create");
            AllocationTracker tracker = script.AllocationTracker;
            long before = tracker.CurrentBytes;
            Table empty = script.CallValues(create, LuaValue.NewInteger(0)).Table;
            long emptyBytes = tracker.CurrentBytes - before;
            await Assert.That(empty.Length).IsEqualTo(0).ConfigureAwait(false);

            const int sequenceHint = 16;
            const int recordHint = 4;
            before = tracker.CurrentBytes;
            Table hinted = script
                .CallValues(
                    create,
                    LuaValue.NewInteger(sequenceHint),
                    LuaValue.NewInteger(recordHint)
                )
                .Table;
            long reserved = tracker.CurrentBytes;
            await Assert.That(reserved - before).IsGreaterThan(emptyBytes).ConfigureAwait(false);
            await Assert.That(hinted.Length).IsEqualTo(0).ConfigureAwait(false);

            for (int i = 1; i <= sequenceHint; i++)
            {
                hinted.Set(i, LuaValue.True);
            }

            hinted.Set("first", LuaValue.True);
            hinted.Set("second", LuaValue.True);
            hinted.Set("third", LuaValue.True);
            hinted.Set("fourth", LuaValue.True);
            await Assert.That(tracker.CurrentBytes).IsEqualTo(reserved).ConfigureAwait(false);
            await Assert.That(hinted.Length).IsEqualTo(sequenceHint).ConfigureAwait(false);

            Table sequence = script.CallValues(create, LuaValue.NewInteger(sequenceHint)).Table;
            long sequenceReserved = tracker.CurrentBytes;
            sequence.Set("first", LuaValue.True);
            await Assert
                .That(tracker.CurrentBytes)
                .IsGreaterThan(sequenceReserved)
                .ConfigureAwait(false);

            sequence.Set("second", LuaValue.True);
            sequence.Set("third", LuaValue.True);
            sequence.Set("fourth", LuaValue.True);
            sequence.Set("fifth", LuaValue.True);
            long withRecords = tracker.CurrentBytes;
            await Assert.That(withRecords).IsGreaterThan(sequenceReserved).ConfigureAwait(false);

            for (int i = 1; i <= sequenceHint; i++)
            {
                sequence.Set(i, LuaValue.True);
            }

            await Assert.That(tracker.CurrentBytes).IsEqualTo(withRecords).ConfigureAwait(false);
        }
    }
}
