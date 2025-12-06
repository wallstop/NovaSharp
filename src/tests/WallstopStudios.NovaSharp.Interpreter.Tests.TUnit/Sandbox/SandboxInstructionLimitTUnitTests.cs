namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Sandbox
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    /// <summary>
    /// Tests for <see cref="SandboxOptions"/> instruction limits.
    /// </summary>
    public sealed class SandboxInstructionLimitTUnitTests
    {
        [Test]
        public async Task InstructionLimitExceededThrowsSandboxViolationException()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxInstructions = 10 };
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            SandboxViolationException ex = await Assert
                .That(() => script.DoString("while true do end"))
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);

            await Assert
                .That(ex.ViolationType)
                .IsEqualTo(SandboxViolationType.InstructionLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(ex.ConfiguredLimit).IsEqualTo(10).ConfigureAwait(false);
        }

        [Test]
        public async Task ScriptExecutesWithinInstructionLimit()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxInstructions = 1000 };
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            // Simple script that should complete well under 1000 instructions
            DynValue result = script.DoString("return 1 + 2");

            await Assert.That(result.Number).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task UnlimitedInstructionsDoesNotThrow()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxInstructions = 0 }; // 0 = unlimited
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            // Run a moderately long script
            DynValue result = script.DoString(
                @"
                local sum = 0
                for i = 1, 100 do
                    sum = sum + i
                end
                return sum
            "
            );

            await Assert.That(result.Number).IsEqualTo(5050).ConfigureAwait(false);
        }

        [Test]
        public async Task InstructionLimitCallbackCanAllowContinuation()
        {
            int callbackInvocations = 0;
            SandboxOptions sandbox = new SandboxOptions
            {
                MaxInstructions = 50,
                OnInstructionLimitExceeded = (s, count) =>
                {
                    callbackInvocations++;
                    // Allow continuation up to 3 times
                    return callbackInvocations < 3;
                },
            };
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            // Script that needs more than 150 instructions (3 * 50) should eventually fail
            await Assert
                .That(() =>
                    script.DoString(
                        @"
                    local sum = 0
                    for i = 1, 1000 do
                        sum = sum + i
                    end
                    return sum
                "
                    )
                )
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);

            await Assert.That(callbackInvocations).IsGreaterThanOrEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task InstructionLimitCallbackResetAllowsFullCompletion()
        {
            int callbackInvocations = 0;
            SandboxOptions sandbox = new SandboxOptions
            {
                MaxInstructions = 100,
                OnInstructionLimitExceeded = (s, count) =>
                {
                    callbackInvocations++;
                    return true; // Always allow continuation (resets counter)
                },
            };
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            // Should complete because callback always allows reset
            DynValue result = script.DoString(
                @"
                local sum = 0
                for i = 1, 500 do
                    sum = sum + i
                end
                return sum
            "
            );

            await Assert.That(result.Number).IsEqualTo(125250).ConfigureAwait(false);
            await Assert.That(callbackInvocations).IsGreaterThan(0).ConfigureAwait(false);
        }

        [Test]
        public async Task HasInstructionLimitReturnsTrueWhenSet()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxInstructions = 100 };

            await Assert.That(sandbox.HasInstructionLimit).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task HasInstructionLimitReturnsFalseWhenUnlimited()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxInstructions = 0 };

            await Assert.That(sandbox.HasInstructionLimit).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task NegativeInstructionLimitTreatedAsUnlimited()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxInstructions = -100 };

            await Assert.That(sandbox.MaxInstructions).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(sandbox.HasInstructionLimit).IsFalse().ConfigureAwait(false);
        }
    }
}
