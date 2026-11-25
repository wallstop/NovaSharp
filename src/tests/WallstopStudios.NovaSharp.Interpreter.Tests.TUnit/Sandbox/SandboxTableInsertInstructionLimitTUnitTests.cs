namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Sandbox
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    public sealed class SandboxTableInsertInstructionLimitTUnitTests
    {
        private const long Limit = 64;

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task IndexedInsertConsumesInstructionBudget(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version, new SandboxOptions { MaxInstructions = Limit });

            SandboxViolationException error = await Assert
                .That(() => script.DoString("table.insert(values, 1, 'new')"))
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);

            await Assert
                .That(error.ViolationType)
                .IsEqualTo(SandboxViolationType.InstructionLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(error.ConfiguredLimit).IsEqualTo(Limit).ConfigureAwait(false);
            await Assert.That(error.ActualValue).IsEqualTo(Limit + 1).ConfigureAwait(false);

            Table backing = script.Globals.Get("backing").Table;
            await Assert.That(backing.Get(129).String).IsEqualTo("tail").ConfigureAwait(false);
            await Assert.That(backing.Get(1).IsNil).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task IndexedInsertCanRenewInstructionBudget(LuaCompatibilityVersion version)
        {
            int renewals = 0;
            Script script = CreateScript(
                version,
                new SandboxOptions
                {
                    MaxInstructions = Limit,
                    OnInstructionLimitExceeded = (_, _) =>
                    {
                        renewals++;
                        return true;
                    },
                }
            );

            script.DoString("table.insert(values, 1, 'new')");

            await Assert.That(renewals).IsGreaterThan(1).ConfigureAwait(false);
            Table backing = script.Globals.Get("backing").Table;
            await Assert.That(backing.Get(129).String).IsEqualTo("tail").ConfigureAwait(false);
            await Assert.That(backing.Get(1).String).IsEqualTo("new").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DirectIndexedInsertUsesAnIndependentBudget(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version, new SandboxOptions { MaxInstructions = Limit });
            LuaValue insert = script.Globals.Get("table").Table.Get("insert");
            LuaValue values = script.Globals.Get("values");

            SandboxViolationException error = await Assert
                .That(() =>
                    script.CallValues(
                        insert,
                        values,
                        LuaValue.NewInteger(1),
                        LuaValue.NewString("new")
                    )
                )
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);

            await Assert
                .That(error.ViolationType)
                .IsEqualTo(SandboxViolationType.InstructionLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(error.ActualValue).IsEqualTo(Limit + 1).ConfigureAwait(false);

            script.CallValues(insert, values, LuaValue.NewString("append"));
            await Assert
                .That(script.Globals.Get("backing").Table.Get(129).String)
                .IsEqualTo("append")
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DirectInsertCountsLengthCallbackAndIndexedWritesTogether(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version, new SandboxOptions(), length: 12);
            script.DoString(
                """
                getmetatable(values).__len = function()
                    local length = 0
                    for i = 1, 3 do length = length + 1 end
                    return length * 4
                end
                """
            );
            int renewals = 0;
            script.Options.Sandbox = new SandboxOptions
            {
                MaxInstructions = Limit,
                OnInstructionLimitExceeded = (_, _) =>
                {
                    renewals++;
                    return true;
                },
            };

            LuaValue insert = script.Globals.Get("table").Table.Get("insert");
            LuaValue values = script.Globals.Get("values");
            script.CallValues(insert, values, LuaValue.NewInteger(1), LuaValue.NewString("new"));

            await Assert.That(renewals).IsEqualTo(1).ConfigureAwait(false);
            Table backing = script.Globals.Get("backing").Table;
            await Assert.That(backing.Get(1).String).IsEqualTo("new").ConfigureAwait(false);
            await Assert.That(backing.Get(13).String).IsEqualTo("tail").ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task IndexedInsertChargesAcrossCallsInOneVmEntry(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(
                version,
                new SandboxOptions { MaxInstructions = Limit },
                length: 10
            );

            script.DoString("table.insert(values, 1, 'first')");
            script.DoString("table.insert(values, 1, 'second')");

            SandboxViolationException error = await Assert
                .That(() =>
                    script.DoString(
                        """
                        table.insert(values, 1, 'first')
                        table.insert(values, 1, 'second')
                        """
                    )
                )
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);

            await Assert
                .That(error.ViolationType)
                .IsEqualTo(SandboxViolationType.InstructionLimitExceeded)
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ProtectedInsertNeedsHostRenewalToContinue(LuaCompatibilityVersion version)
        {
            int overruns = 0;
            Script script = CreateScript(
                version,
                new SandboxOptions
                {
                    MaxInstructions = Limit,
                    OnInstructionLimitExceeded = (_, _) => ++overruns > 1,
                }
            );

            LuaValue result = script.DoString(
                """
                local ok = pcall(table.insert, values, 1, 'new')
                return ok
                """
            );

            await Assert.That(result).IsEqualTo(LuaValue.False).ConfigureAwait(false);
            await Assert.That(overruns).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(script.Globals.Get("backing").Table.Get(1).IsNil)
                .IsTrue()
                .ConfigureAwait(false);
        }

        private static Script CreateScript(
            LuaCompatibilityVersion version,
            SandboxOptions sandbox,
            int length = 128
        )
        {
            Script script = new(new ScriptOptions { CompatibilityVersion = version });
            script.Globals.Set("sequenceLength", LuaValue.NewInteger(length));
            script.DoString(
                """
                backing = {[sequenceLength] = 'tail'}
                values = setmetatable({}, {
                    __len = function() return sequenceLength end,
                    __index = backing,
                    __newindex = backing,
                })
                """
            );
            script.Options.Sandbox = sandbox;
            return script;
        }
    }
}
