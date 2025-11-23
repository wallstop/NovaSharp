namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.VsCodeDebugger.SDK;
    using NUnit.Framework;

    [TestFixture]
    public sealed class UtilitiesTests
    {
        [Test]
        public void ExpandVariablesReplacesUnderscoredPlaceholders()
        {
            string format = "Hello {_name}, result: {_result}!";
            string actual = Utilities.ExpandVariables(
                format,
                new { _name = "NovaSharp", _result = 42 }
            );

            Assert.That(actual, Is.EqualTo("Hello NovaSharp, result: 42!"));
        }

        [Test]
        public void ExpandVariablesLeavesUnknownPlaceholdersIntact()
        {
            string format = "Command: {_cmd}, Missing: {_missing}";
            string actual = Utilities.ExpandVariables(format, new { _cmd = "run" });

            Assert.That(actual, Is.EqualTo("Command: run, Missing: {_missing: not found}"));
        }

        [Test]
        public void ExpandVariablesRespectsUnderscoredOnlyToggle()
        {
            string format = "{_hidden} and {visible}";
            string actual = Utilities.ExpandVariables(
                format,
                new { _hidden = "secret", visible = "shown" },
                underscoredOnly: false
            );

            Assert.That(actual, Is.EqualTo("secret and shown"));
        }

        [Test]
        public void MakeRelativePathStripsSharedPrefix()
        {
            string dir = "/home/user/project";
            string file = "/home/user/project/scripts/main.lua";

            string relative = Utilities.MakeRelativePath(dir, file);

            Assert.That(relative, Is.EqualTo("scripts/main.lua"));
        }

        [Test]
        public void MakeRelativePathReturnsOriginalWhenOutsideRoot()
        {
            string dir = "/home/user/project";
            string file = "/home/user/other/file.lua";

            string relative = Utilities.MakeRelativePath(dir, file);

            Assert.That(relative, Is.EqualTo(file));
        }
    }
}
