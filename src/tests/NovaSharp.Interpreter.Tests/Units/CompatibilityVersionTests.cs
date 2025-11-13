namespace NovaSharp.Interpreter.Tests.Units
{
    using NUnit.Framework;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;

    [TestFixture]
    public class CompatibilityVersionTests
    {
        [SetUp]
        public void ResetGlobalCompatibility()
        {
            Script.GlobalOptions.CompatibilityVersion = LuaCompatibilityVersion.Latest;
        }

        [Test]
        public void DefaultsToLatestCompatibilityVersion()
        {
            Assert.That(
                Script.GlobalOptions.CompatibilityVersion,
                Is.EqualTo(LuaCompatibilityVersion.Latest)
            );

            Script script = new();
            Assert.That(script.CompatibilityVersion, Is.EqualTo(LuaCompatibilityVersion.Latest));
        }

        [Test]
        public void ScriptOptionsCanOverrideCompatibilityVersion()
        {
            Script.GlobalOptions.CompatibilityVersion = LuaCompatibilityVersion.Lua54;
            Script script = new();

            Assert.That(script.CompatibilityVersion, Is.EqualTo(LuaCompatibilityVersion.Lua54));

            script.Options.CompatibilityVersion = LuaCompatibilityVersion.Lua53;
            Assert.That(script.CompatibilityVersion, Is.EqualTo(LuaCompatibilityVersion.Lua53));
        }
    }
}
