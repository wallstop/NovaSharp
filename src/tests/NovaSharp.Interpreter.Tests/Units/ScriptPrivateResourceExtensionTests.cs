namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptPrivateResourceExtensionTests
    {
        [Test]
        public void CheckScriptOwnershipAllowsResourcesOwnedBySameScript()
        {
            Script script = new();
            TestResource container = new(script);
            DynValue dynValue = DynValue.NewTable(script);

            Assert.DoesNotThrow(() => container.CheckScriptOwnership(dynValue));
        }

        [Test]
        public void CheckScriptOwnershipThrowsWhenResourcesAreOwnedByDifferentScripts()
        {
            Script scriptA = new();
            Script scriptB = new();
            TestResource container = new(scriptA);
            DynValue dynValue = DynValue.NewTable(scriptB);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                container.CheckScriptOwnership(dynValue)
            );

            Assert.That(exception.Message, Does.Contain("resources owned by different scripts"));
        }

        [Test]
        public void CheckScriptOwnershipThrowsWhenSharedResourceReceivesPrivateResource()
        {
            TestResource sharedContainer = new(owner: null);
            DynValue dynValue = DynValue.NewTable(new Script());

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                sharedContainer.CheckScriptOwnership(dynValue)
            );

            Assert.That(
                exception.Message,
                Does.Contain("script private resource on a shared resource")
            );
        }

        [Test]
        public void CheckScriptOwnershipValidatesEveryValueInArray()
        {
            Script scriptA = new();
            Script scriptB = new();
            TestResource container = new(scriptA);

            DynValue[] values = new[] { DynValue.NewTable(scriptA), DynValue.NewTable(scriptB) };

            Assert.Throws<ScriptRuntimeException>(
                () => container.CheckScriptOwnership(values),
                "Second entry should trigger the mismatch."
            );
        }

        [Test]
        public void CheckScriptOwnershipIgnoresNullDynValues()
        {
            TestResource container = new(new Script());
            Assert.DoesNotThrow(() => container.CheckScriptOwnership((DynValue)null));
        }

        [Test]
        public void CheckScriptOwnershipAllowsNonPrivateValues()
        {
            TestResource container = new(new Script());
            DynValue constant = DynValue.NewNumber(123);

            Assert.DoesNotThrow(() => container.CheckScriptOwnership(constant));
        }

        [Test]
        public void CheckScriptOwnershipGuardsScriptParameter()
        {
            Script owner = new();
            Script other = new();
            TestResource container = new(owner);

            Assert.DoesNotThrow(() => container.CheckScriptOwnership(owner));
            Assert.DoesNotThrow(() => container.CheckScriptOwnership(script: null));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                container.CheckScriptOwnership(other)
            )!;

            Assert.That(exception.Message, Does.Contain("another script"));
        }

        private sealed class TestResource : IScriptPrivateResource
        {
            public TestResource(Script owner)
            {
                OwnerScript = owner;
            }

            public Script OwnerScript { get; }
        }
    }
}
