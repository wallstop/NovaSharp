namespace NovaSharp.Interpreter.Tests.TUnit.Sandbox
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Sandboxing;

    /// <summary>
    /// Tests for <see cref="SandboxOptions"/> recursion depth limits.
    /// </summary>
    public sealed class SandboxRecursionLimitTUnitTests
    {
        [Test]
        public async Task RecursionLimitExceededThrowsSandboxViolationException()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxCallStackDepth = 5 };
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            // Define a deeply recursive function
            script.DoString(
                @"
                function recurse(n)
                    if n <= 0 then
                        return 0
                    end
                    return 1 + recurse(n - 1)
                end
            "
            );

            SandboxViolationException ex = await Assert
                .That(() => script.DoString("return recurse(100)"))
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);

            await Assert
                .That(ex.ViolationType)
                .IsEqualTo(SandboxViolationType.RecursionLimitExceeded)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ScriptExecutesWithinRecursionLimit()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxCallStackDepth = 100 };
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            // Shallow recursion that should complete
            script.DoString(
                @"
                function factorial(n)
                    if n <= 1 then
                        return 1
                    end
                    return n * factorial(n - 1)
                end
            "
            );

            DynValue result = script.DoString("return factorial(10)");

            await Assert.That(result.Number).IsEqualTo(3628800).ConfigureAwait(false);
        }

        [Test]
        public async Task UnlimitedRecursionDoesNotThrow()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxCallStackDepth = 0 }; // 0 = unlimited
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            script.DoString(
                @"
                function recurse(n)
                    if n <= 0 then
                        return 0
                    end
                    return 1 + recurse(n - 1)
                end
            "
            );

            // Should complete (limited by internal stack, not sandbox)
            DynValue result = script.DoString("return recurse(50)");

            await Assert.That(result.Number).IsEqualTo(50).ConfigureAwait(false);
        }

        [Test]
        public async Task RecursionLimitCallbackCanAllowContinuation()
        {
            int callbackInvocations = 0;
            SandboxOptions sandbox = new SandboxOptions
            {
                MaxCallStackDepth = 5,
                OnRecursionLimitExceeded = (s, depth) =>
                {
                    callbackInvocations++;
                    // Allow continuation a few times
                    return callbackInvocations < 10;
                },
            };
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            script.DoString(
                @"
                function recurse(n)
                    if n <= 0 then
                        return 0
                    end
                    return 1 + recurse(n - 1)
                end
            "
            );

            // Should eventually fail after callback returns false
            await Assert
                .That(() => script.DoString("return recurse(100)"))
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);

            await Assert.That(callbackInvocations).IsGreaterThanOrEqualTo(10).ConfigureAwait(false);
        }

        [Test]
        public async Task HasCallStackDepthLimitReturnsTrueWhenSet()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxCallStackDepth = 50 };

            await Assert.That(sandbox.HasCallStackDepthLimit).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task HasCallStackDepthLimitReturnsFalseWhenUnlimited()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxCallStackDepth = 0 };

            await Assert.That(sandbox.HasCallStackDepthLimit).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task NegativeCallStackDepthTreatedAsUnlimited()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxCallStackDepth = -10 };

            await Assert.That(sandbox.MaxCallStackDepth).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(sandbox.HasCallStackDepthLimit).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task MutualRecursionCountedCorrectly()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxCallStackDepth = 10 };
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            // Use non-tail-call recursion to avoid TCO
            script.DoString(
                @"
                function isEven(n)
                    if n == 0 then return true end
                    local result = isOdd(n - 1)
                    return result  -- Local variable prevents TCO
                end
                
                function isOdd(n)
                    if n == 0 then return false end
                    local result = isEven(n - 1)
                    return result  -- Local variable prevents TCO
                end
            "
            );

            // n=200 requires 200+ call frames without TCO
            // With limit of 10, this should fail
            await Assert
                .That(() => script.DoString("return isEven(200)"))
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);
        }
    }
}
