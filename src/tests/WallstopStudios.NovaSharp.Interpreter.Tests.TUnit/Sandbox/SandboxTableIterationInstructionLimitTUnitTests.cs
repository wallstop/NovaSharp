namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Sandbox
{
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    public sealed class SandboxTableIterationInstructionLimitTUnitTests
    {
        [Test]
        [Arguments("foreach")]
        [Arguments("foreachi")]
        public async Task LegacyIterationConsumesInstructionBudget(string name)
        {
            const long limit = 64;
            Script script = new(LuaCompatibilityVersion.Lua51);
            script.DoString(
                """
                values = {}
                for i = 1, 128 do values[i] = i end
                """
            );
            script.Globals.Set("iterate", script.Globals.Get("table").Table.Get(name));
            script.Options.Sandbox = new SandboxOptions { MaxInstructions = limit };

            SandboxViolationException error = await Assert
                .That(() => script.DoString("iterate(values, function() end)"))
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
