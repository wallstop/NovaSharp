namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Sandbox
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    public sealed class SandboxTableIterationInstructionLimitTUnitTests
    {
        private const long Limit = 64;

        [Test]
        [Arguments("foreach", false)]
        [Arguments("foreach", true)]
        [Arguments("foreachi", false)]
        [Arguments("foreachi", true)]
        public async Task LegacyIterationConsumesInstructionBudget(string name, bool direct)
        {
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
            script.Options.Sandbox = new SandboxOptions { MaxInstructions = Limit };

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
            await Assert.That(error.ConfiguredLimit).IsEqualTo(Limit).ConfigureAwait(false);
            await Assert.That(error.ActualValue).IsEqualTo(Limit + 1).ConfigureAwait(false);
        }

        [Test]
        [Arguments(8, false, false)]
        [Arguments(80, true, false)]
        [Arguments(8, false, true)]
        [Arguments(80, true, true)]
        public async Task NativeWorkDuringRenewalCannotRenewRecursively(
            int nestedCount,
            bool nestedExhaustionExpected,
            bool useCoroutine
        )
        {
            Script script = new(LuaCompatibilityVersion.Lua51);
            LuaValue iterate = script.Globals.Get("table").Table.Get("foreachi");
            LuaValue action = script.DoString("return function() end");
            LuaValue coroutineBody = script.DoString(
                "return function(visit, values, action) visit(values, action) end"
            );
            LuaValue outer = LuaValue.NewTable(CreateSequence(script, 128));
            LuaValue nested = LuaValue.NewTable(CreateSequence(script, nestedCount));
            int renewals = 0;
            int recursiveRenewals = 0;
            int nestedFailures = 0;
            bool inRenewal = false;
            SandboxViolationException nestedError = null;
            script.Options.Sandbox = new SandboxOptions
            {
                MaxInstructions = Limit,
                OnInstructionLimitExceeded = (_, _) =>
                {
                    if (inRenewal)
                    {
                        recursiveRenewals++;
                        return false;
                    }

                    renewals++;
                    inRenewal = true;
                    using DeferredActionScope leaveRenewal = DeferredActionScope.Run(() =>
                        inRenewal = false
                    );
                    try
                    {
                        if (useCoroutine)
                        {
                            script
                                .CreateCoroutine(coroutineBody)
                                .Coroutine.Resume(new[] { iterate, nested, action });
                        }
                        else
                        {
                            script.CallValues(iterate, nested, action);
                        }
                    }
                    catch (SandboxViolationException error)
                    {
                        nestedFailures++;
                        nestedError = error;
                    }
                    return true;
                },
            };

            script.CallValues(iterate, outer, action);

            await Assert.That(renewals).IsGreaterThan(0).ConfigureAwait(false);
            await Assert.That(recursiveRenewals).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(nestedFailures)
                .IsEqualTo(nestedExhaustionExpected ? renewals : 0)
                .ConfigureAwait(false);
            if (nestedExhaustionExpected)
            {
                await Assert
                    .That(nestedError!.ViolationType)
                    .IsEqualTo(SandboxViolationType.InstructionLimitExceeded)
                    .ConfigureAwait(false);
                await Assert
                    .That(nestedError.ActualValue)
                    .IsEqualTo(Limit + 1)
                    .ConfigureAwait(false);
            }
        }

        private static Table CreateSequence(Script script, int count)
        {
            Table table = new(script);
            for (int i = 1; i <= count; i++)
            {
                table.Set(i, LuaValue.NewInteger(i));
            }

            return table;
        }
    }
}
