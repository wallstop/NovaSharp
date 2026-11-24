namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Sandbox
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Core;

    public sealed class LuaSandboxOptionsTUnitTests
    {
        [Test]
        [Arguments(-1L, 0L)]
        [Arguments(0L, 0L)]
        [Arguments(1L, 1L)]
        public async Task LimitsTreatNonpositiveValuesAsUnlimited(long requested, long expected)
        {
            LuaSandboxOptions options = new LuaSandboxOptions
            {
                MaxInstructions = requested,
                MaxCallStackDepth = (int)requested,
                MaxMemoryBytes = requested,
                MaxCoroutines = (int)requested,
            };

            await Assert.That(options.MaxInstructions).IsEqualTo(expected).ConfigureAwait(false);
            await Assert
                .That(options.MaxCallStackDepth)
                .IsEqualTo((int)expected)
                .ConfigureAwait(false);
            await Assert.That(options.MaxMemoryBytes).IsEqualTo(expected).ConfigureAwait(false);
            await Assert.That(options.MaxCoroutines).IsEqualTo((int)expected).ConfigureAwait(false);
            await Assert
                .That(options.HasInstructionLimit)
                .IsEqualTo(expected > 0)
                .ConfigureAwait(false);
            await Assert
                .That(options.HasCallStackDepthLimit)
                .IsEqualTo(expected > 0)
                .ConfigureAwait(false);
            await Assert.That(options.HasMemoryLimit).IsEqualTo(expected > 0).ConfigureAwait(false);
            await Assert
                .That(options.HasCoroutineLimit)
                .IsEqualTo(expected > 0)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task CopyAndSnapshotsPreserveIndependentRestrictionLists()
        {
            LuaSandboxOptions original = new LuaSandboxOptions
            {
                MaxInstructions = 10,
                MaxCallStackDepth = 20,
                MaxMemoryBytes = 30,
                MaxCoroutines = 40,
            };
            original.RestrictModules("os", "os").RestrictFunctions("load", "load");
            IReadOnlyCollection<string> moduleSnapshot = original.RestrictedModules;
            IReadOnlyCollection<string> functionSnapshot = original.RestrictedFunctions;
            LuaSandboxOptions copy = new LuaSandboxOptions(original);

            original.AllowModule("os").AllowFunction("load");
            original.MaxInstructions = 0;

            await Assert.That(moduleSnapshot).Contains("os").ConfigureAwait(false);
            await Assert.That(functionSnapshot).Contains("load").ConfigureAwait(false);
            await Assert.That(original.RestrictedModules.Count).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(original.RestrictedFunctions.Count)
                .IsEqualTo(0)
                .ConfigureAwait(false);
            await Assert.That(copy.RestrictedModules.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(copy.RestrictedFunctions.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(copy.IsModuleRestricted("os")).IsTrue().ConfigureAwait(false);
            await Assert.That(copy.IsModuleRestricted("OS")).IsFalse().ConfigureAwait(false);
            await Assert.That(copy.IsModuleRestricted("load")).IsFalse().ConfigureAwait(false);
            await Assert.That(copy.IsFunctionRestricted("load")).IsTrue().ConfigureAwait(false);
            await Assert.That(copy.IsFunctionRestricted("LOAD")).IsFalse().ConfigureAwait(false);
            await Assert.That(copy.IsFunctionRestricted("os")).IsFalse().ConfigureAwait(false);
            await Assert.That(copy.MaxInstructions).IsEqualTo(10L).ConfigureAwait(false);
            await Assert.That(copy.MaxCallStackDepth).IsEqualTo(20).ConfigureAwait(false);
            await Assert.That(copy.MaxMemoryBytes).IsEqualTo(30L).ConfigureAwait(false);
            await Assert.That(copy.MaxCoroutines).IsEqualTo(40).ConfigureAwait(false);
        }

        [Test]
        [Arguments((string)null)]
        [Arguments("")]
        [Arguments(" \t")]
        public async Task BlankNamesCannotBeAddedToRestrictionLists(string name)
        {
            LuaSandboxOptions options = new LuaSandboxOptions()
                .RestrictModule("os")
                .RestrictFunction("load");
            ArgumentException module = Assert.Throws<ArgumentException>(() =>
                options.RestrictModule(name)
            );
            ArgumentException function = Assert.Throws<ArgumentException>(() =>
                options.RestrictFunction(name)
            );

            options.AllowModule(name).AllowFunction(name);

            await Assert.That(module.ParamName).IsEqualTo("moduleName").ConfigureAwait(false);
            await Assert.That(function.ParamName).IsEqualTo("functionName").ConfigureAwait(false);
            await Assert.That(options.IsModuleRestricted(name)).IsFalse().ConfigureAwait(false);
            await Assert.That(options.IsFunctionRestricted(name)).IsFalse().ConfigureAwait(false);
            await Assert.That(options.IsModuleRestricted("os")).IsTrue().ConfigureAwait(false);
            await Assert.That(options.IsFunctionRestricted("load")).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public void CopyAndBulkRestrictionsRejectNullArguments()
        {
            LuaSandboxOptions options = new LuaSandboxOptions();

            Assert.Throws<ArgumentNullException>(() => _ = new LuaSandboxOptions(null));
            Assert.Throws<ArgumentNullException>(() => options.RestrictModules(null));
            Assert.Throws<ArgumentNullException>(() => options.RestrictFunctions(null));
        }
    }
}
