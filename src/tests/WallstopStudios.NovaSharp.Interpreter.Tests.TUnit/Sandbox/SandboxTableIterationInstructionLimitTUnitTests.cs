namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Sandbox
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    public sealed class SandboxTableIterationInstructionLimitTUnitTests
    {
        [Test]
        [Arguments("foreach", false)]
        [Arguments("foreach", true)]
        [Arguments("foreachi", false)]
        [Arguments("foreachi", true)]
        public async Task LegacyIterationConsumesInstructionBudget(string name, bool direct)
        {
            const long limit = 64;
            Script script = new(LuaCompatibilityVersion.Lua51);
            script.DoString(
                """
                values = {}
                for i = 1, 128 do values[i] = i end
                """
            );
            LuaValue iterate = script.Globals.Get("table").Table.Get(name);
            LuaValue action = script.DoString("return function() end");
            script.Globals.Set("iterate", iterate);
            script.Options.Sandbox = new SandboxOptions { MaxInstructions = limit };

            SandboxViolationException error = await Assert
                .That(() =>
                    direct
                        ? script.CallValues(iterate, script.Globals.Get("values"), action)
                        : script.DoString("iterate(values, function() end)")
                )
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);

            await Assert
                .That(error.ViolationType)
                .IsEqualTo(SandboxViolationType.InstructionLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(error.ConfiguredLimit).IsEqualTo(limit).ConfigureAwait(false);
            await Assert.That(error.ActualValue).IsEqualTo(limit + 1).ConfigureAwait(false);
        }
    }
}
