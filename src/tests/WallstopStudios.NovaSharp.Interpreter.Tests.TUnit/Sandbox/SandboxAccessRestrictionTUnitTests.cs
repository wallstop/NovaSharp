namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Sandbox
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    /// <summary>
    /// Tests for <see cref="SandboxOptions"/> function and module restrictions.
    /// </summary>
    public sealed class SandboxAccessRestrictionTUnitTests
    {
        [Test]
        public async Task RestrictedFunctionLoadThrowsSandboxViolationException()
        {
            SandboxOptions sandbox = new SandboxOptions().RestrictFunction("load");
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            SandboxViolationException ex = await Assert
                .That(() => script.DoString("return load('return 42')()"))
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);

            await Assert
                .That(ex.ViolationType)
                .IsEqualTo(SandboxViolationType.FunctionAccessDenied)
                .ConfigureAwait(false);
            await Assert.That(ex.DeniedAccessName).IsEqualTo("load").ConfigureAwait(false);
        }

        [Test]
        public async Task RestrictedFunctionLoadfileThrowsSandboxViolationException()
        {
            SandboxOptions sandbox = new SandboxOptions().RestrictFunction("loadfile");
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            SandboxViolationException ex = await Assert
                .That(() => script.DoString("return loadfile('test.lua')"))
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);

            await Assert
                .That(ex.ViolationType)
                .IsEqualTo(SandboxViolationType.FunctionAccessDenied)
                .ConfigureAwait(false);
            await Assert.That(ex.DeniedAccessName).IsEqualTo("loadfile").ConfigureAwait(false);
        }

        [Test]
        public async Task RestrictedFunctionDofileThrowsSandboxViolationException()
        {
            SandboxOptions sandbox = new SandboxOptions().RestrictFunction("dofile");
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            SandboxViolationException ex = await Assert
                .That(() => script.DoString("dofile('test.lua')"))
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);

            await Assert
                .That(ex.ViolationType)
                .IsEqualTo(SandboxViolationType.FunctionAccessDenied)
                .ConfigureAwait(false);
            await Assert.That(ex.DeniedAccessName).IsEqualTo("dofile").ConfigureAwait(false);
        }

        [Test]
        public async Task UnrestrictedFunctionExecutesNormally()
        {
            SandboxOptions sandbox = new SandboxOptions().RestrictFunction("dofile"); // Only restrict dofile
            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            // load should work
            DynValue result = script.DoString("return load('return 42')()");

            await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [Test]
        public async Task FunctionAccessCallbackCanAllowAccess()
        {
            bool callbackInvoked = false;
            SandboxOptions sandbox = new SandboxOptions
            {
                OnFunctionAccessDenied = (s, name) =>
                {
                    callbackInvoked = true;
                    return true; // Allow access
                },
            };
            sandbox.RestrictFunction("load");

            ScriptOptions options = new ScriptOptions { Sandbox = sandbox };
            Script script = new Script(options);

            // Should succeed because callback allows it
            DynValue result = script.DoString("return load('return 99')()");

            await Assert.That(result.Number).IsEqualTo(99).ConfigureAwait(false);
            await Assert.That(callbackInvoked).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task RestrictModulesMethodChaining()
        {
            SandboxOptions sandbox = new SandboxOptions().RestrictModules("io", "os", "debug");

            await Assert.That(sandbox.IsModuleRestricted("io")).IsTrue().ConfigureAwait(false);
            await Assert.That(sandbox.IsModuleRestricted("os")).IsTrue().ConfigureAwait(false);
            await Assert.That(sandbox.IsModuleRestricted("debug")).IsTrue().ConfigureAwait(false);
            await Assert.That(sandbox.IsModuleRestricted("string")).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task RestrictFunctionsMethodChaining()
        {
            SandboxOptions sandbox = new SandboxOptions().RestrictFunctions(
                "load",
                "loadfile",
                "dofile"
            );

            await Assert.That(sandbox.IsFunctionRestricted("load")).IsTrue().ConfigureAwait(false);
            await Assert
                .That(sandbox.IsFunctionRestricted("loadfile"))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(sandbox.IsFunctionRestricted("dofile"))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(sandbox.IsFunctionRestricted("print"))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AllowModuleRemovesRestriction()
        {
            SandboxOptions sandbox = new SandboxOptions()
                .RestrictModules("io", "os")
                .AllowModule("io");

            await Assert.That(sandbox.IsModuleRestricted("io")).IsFalse().ConfigureAwait(false);
            await Assert.That(sandbox.IsModuleRestricted("os")).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task AllowFunctionRemovesRestriction()
        {
            SandboxOptions sandbox = new SandboxOptions()
                .RestrictFunctions("load", "dofile")
                .AllowFunction("load");

            await Assert.That(sandbox.IsFunctionRestricted("load")).IsFalse().ConfigureAwait(false);
            await Assert
                .That(sandbox.IsFunctionRestricted("dofile"))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task GetRestrictedModulesReturnsConfiguredValues()
        {
            SandboxOptions sandbox = new SandboxOptions().RestrictModules("io", "os");

            System.Collections.Generic.IReadOnlyCollection<string> modules =
                sandbox.GetRestrictedModules();

            await Assert.That(modules.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(modules).Contains("io").ConfigureAwait(false);
            await Assert.That(modules).Contains("os").ConfigureAwait(false);
        }

        [Test]
        public async Task GetRestrictedFunctionsReturnsConfiguredValues()
        {
            SandboxOptions sandbox = new SandboxOptions().RestrictFunctions("load", "loadfile");

            System.Collections.Generic.IReadOnlyCollection<string> functions =
                sandbox.GetRestrictedFunctions();

            await Assert.That(functions.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(functions).Contains("load").ConfigureAwait(false);
            await Assert.That(functions).Contains("loadfile").ConfigureAwait(false);
        }

        [Test]
        public async Task HasModuleRestrictionsReturnsTrueWhenConfigured()
        {
            SandboxOptions sandbox = new SandboxOptions().RestrictModule("io");

            await Assert.That(sandbox.HasModuleRestrictions).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task HasModuleRestrictionsReturnsFalseWhenEmpty()
        {
            SandboxOptions sandbox = new SandboxOptions();

            await Assert.That(sandbox.HasModuleRestrictions).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task HasFunctionRestrictionsReturnsTrueWhenConfigured()
        {
            SandboxOptions sandbox = new SandboxOptions().RestrictFunction("load");

            await Assert.That(sandbox.HasFunctionRestrictions).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task HasFunctionRestrictionsReturnsFalseWhenEmpty()
        {
            SandboxOptions sandbox = new SandboxOptions();

            await Assert.That(sandbox.HasFunctionRestrictions).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task RestrictModuleWithNullThrowsArgumentException()
        {
            SandboxOptions sandbox = new SandboxOptions();

            await Assert
                .That(() => sandbox.RestrictModule(null))
                .Throws<ArgumentException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task RestrictFunctionWithNullThrowsArgumentException()
        {
            SandboxOptions sandbox = new SandboxOptions();

            await Assert
                .That(() => sandbox.RestrictFunction(null))
                .Throws<ArgumentException>()
                .ConfigureAwait(false);
        }
    }
}
