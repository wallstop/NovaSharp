namespace NovaSharp.RemoteDebugger.Tests.TUnit.Utilities
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using VsCodeUtilities = NovaSharp.VsCodeDebugger.SDK.Utilities;

    public sealed class UtilitiesTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ExpandVariablesReplacesUnderscoredPlaceholders()
        {
            string format = "Hello {_name}, result: {_result}!";
            string actual = VsCodeUtilities.ExpandVariables(
                format,
                new { _name = "NovaSharp", _result = 42 }
            );

            await Assert
                .That(actual)
                .IsEqualTo("Hello NovaSharp, result: 42!")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExpandVariablesLeavesUnknownPlaceholdersIntact()
        {
            string format = "Command: {_cmd}, Missing: {_missing}";
            string actual = VsCodeUtilities.ExpandVariables(format, new { _cmd = "run" });

            await Assert
                .That(actual)
                .IsEqualTo("Command: run, Missing: {_missing: not found}")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExpandVariablesRespectsUnderscoredOnlyToggle()
        {
            string format = "{_hidden} and {visible}";
            string actual = VsCodeUtilities.ExpandVariables(
                format,
                new { _hidden = "secret", visible = "shown" },
                underscoredOnly: false
            );

            await Assert.That(actual).IsEqualTo("secret and shown").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MakeRelativePathStripsSharedPrefix()
        {
            string dir = "/home/user/project";
            string file = "/home/user/project/scripts/main.lua";

            string relative = VsCodeUtilities.MakeRelativePath(dir, file);
            await Assert.That(relative).IsEqualTo("scripts/main.lua").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MakeRelativePathReturnsOriginalWhenOutsideRoot()
        {
            string dir = "/home/user/project";
            string file = "/home/user/other/file.lua";

            string relative = VsCodeUtilities.MakeRelativePath(dir, file);
            await Assert.That(relative).IsEqualTo(file).ConfigureAwait(false);
        }
    }
}
