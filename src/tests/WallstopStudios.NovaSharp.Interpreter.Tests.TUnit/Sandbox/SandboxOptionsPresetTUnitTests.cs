namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Sandbox
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    /// <summary>
    /// Tests for <see cref="SandboxOptions"/> presets and general configuration.
    /// </summary>
    public sealed class SandboxOptionsPresetTUnitTests
    {
        [Test]
        public async Task CreateRestrictivePresetHasCorrectDefaults()
        {
            SandboxOptions sandbox = SandboxOptions.CreateRestrictive();

            await Assert.That(sandbox.MaxInstructions).IsEqualTo(1_000_000).ConfigureAwait(false);
            await Assert.That(sandbox.MaxCallStackDepth).IsEqualTo(256).ConfigureAwait(false);
            await Assert.That(sandbox.IsModuleRestricted("io")).IsTrue().ConfigureAwait(false);
            await Assert.That(sandbox.IsModuleRestricted("os")).IsTrue().ConfigureAwait(false);
            await Assert.That(sandbox.IsModuleRestricted("debug")).IsTrue().ConfigureAwait(false);
            await Assert
                .That(sandbox.IsFunctionRestricted("loadfile"))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(sandbox.IsFunctionRestricted("dofile"))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(sandbox.IsFunctionRestricted("load")).IsTrue().ConfigureAwait(false);
            await Assert
                .That(sandbox.IsFunctionRestricted("loadstring"))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task CreateRestrictivePresetWithCustomLimits()
        {
            SandboxOptions sandbox = SandboxOptions.CreateRestrictive(
                maxInstructions: 500_000,
                maxCallStackDepth: 128
            );

            await Assert.That(sandbox.MaxInstructions).IsEqualTo(500_000).ConfigureAwait(false);
            await Assert.That(sandbox.MaxCallStackDepth).IsEqualTo(128).ConfigureAwait(false);
        }

        [Test]
        public async Task CreateModeratePresetHasCorrectDefaults()
        {
            SandboxOptions sandbox = SandboxOptions.CreateModerate();

            await Assert.That(sandbox.MaxInstructions).IsEqualTo(10_000_000).ConfigureAwait(false);
            await Assert.That(sandbox.MaxCallStackDepth).IsEqualTo(512).ConfigureAwait(false);
            await Assert.That(sandbox.HasModuleRestrictions).IsFalse().ConfigureAwait(false);
            await Assert.That(sandbox.HasFunctionRestrictions).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task CreateModeratePresetWithCustomLimits()
        {
            SandboxOptions sandbox = SandboxOptions.CreateModerate(
                maxInstructions: 5_000_000,
                maxCallStackDepth: 1024
            );

            await Assert.That(sandbox.MaxInstructions).IsEqualTo(5_000_000).ConfigureAwait(false);
            await Assert.That(sandbox.MaxCallStackDepth).IsEqualTo(1024).ConfigureAwait(false);
        }

        [Test]
        public async Task UnrestrictedSingletonHasNoLimits()
        {
            SandboxOptions sandbox = SandboxOptions.Unrestricted;

            await Assert.That(sandbox.MaxInstructions).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(sandbox.MaxCallStackDepth).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(sandbox.HasInstructionLimit).IsFalse().ConfigureAwait(false);
            await Assert.That(sandbox.HasCallStackDepthLimit).IsFalse().ConfigureAwait(false);
            await Assert.That(sandbox.HasModuleRestrictions).IsFalse().ConfigureAwait(false);
            await Assert.That(sandbox.HasFunctionRestrictions).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task CopyConstructorCopiesAllSettings()
        {
            SandboxOptions original = new SandboxOptions
            {
                MaxInstructions = 1000,
                MaxCallStackDepth = 50,
            };
            original.RestrictModules("io", "os");
            original.RestrictFunctions("load");
            original.OnInstructionLimitExceeded = (s, c) => true;
            original.OnRecursionLimitExceeded = (s, d) => true;

            SandboxOptions copy = new SandboxOptions(original);

            await Assert.That(copy.MaxInstructions).IsEqualTo(1000).ConfigureAwait(false);
            await Assert.That(copy.MaxCallStackDepth).IsEqualTo(50).ConfigureAwait(false);
            await Assert.That(copy.IsModuleRestricted("io")).IsTrue().ConfigureAwait(false);
            await Assert.That(copy.IsModuleRestricted("os")).IsTrue().ConfigureAwait(false);
            await Assert.That(copy.IsFunctionRestricted("load")).IsTrue().ConfigureAwait(false);
            await Assert.That(copy.OnInstructionLimitExceeded).IsNotNull().ConfigureAwait(false);
            await Assert.That(copy.OnRecursionLimitExceeded).IsNotNull().ConfigureAwait(false);
        }

        [Test]
        public async Task CopyConstructorCreatesIndependentCopy()
        {
            SandboxOptions original = new SandboxOptions { MaxInstructions = 1000 };
            original.RestrictModule("io");

            SandboxOptions copy = new SandboxOptions(original);

            // Modify the copy
            copy.MaxInstructions = 2000;
            copy.RestrictModule("os");

            // Original should be unchanged
            await Assert.That(original.MaxInstructions).IsEqualTo(1000).ConfigureAwait(false);
            await Assert.That(original.IsModuleRestricted("os")).IsFalse().ConfigureAwait(false);

            // Copy should have new values
            await Assert.That(copy.MaxInstructions).IsEqualTo(2000).ConfigureAwait(false);
            await Assert.That(copy.IsModuleRestricted("os")).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task CopyConstructorWithNullThrowsArgumentNullException()
        {
            await Assert
                .That(() => new SandboxOptions(null))
                .Throws<ArgumentNullException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task DefaultScriptOptionsUsesUnrestrictedSandbox()
        {
            ScriptOptions options = new ScriptOptions();

            await Assert
                .That(options.Sandbox)
                .IsSameReferenceAs(SandboxOptions.Unrestricted)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ScriptOptionsCopyCreatesNewSandboxInstance()
        {
            SandboxOptions sandbox = new SandboxOptions { MaxInstructions = 500 };
            ScriptOptions original = new ScriptOptions { Sandbox = sandbox };

            ScriptOptions copy = new ScriptOptions(original);

            // Should not be the same reference
            await Assert
                .That(copy.Sandbox)
                .IsNotSameReferenceAs(original.Sandbox)
                .ConfigureAwait(false);

            // But should have same values
            await Assert.That(copy.Sandbox.MaxInstructions).IsEqualTo(500).ConfigureAwait(false);
        }

        [Test]
        public async Task ScriptOptionsCopyWithUnrestrictedKeepsReference()
        {
            ScriptOptions original = new ScriptOptions { Sandbox = SandboxOptions.Unrestricted };

            ScriptOptions copy = new ScriptOptions(original);

            // Unrestricted singleton should be preserved
            await Assert
                .That(copy.Sandbox)
                .IsSameReferenceAs(SandboxOptions.Unrestricted)
                .ConfigureAwait(false);
        }
    }
}
