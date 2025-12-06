namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Tap
{
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    public sealed class TapStdinHelperTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ResolveInputPathFallsBackToRuntimeDirectory()
        {
            using TempDirectoryScope tempDirectory = TempDirectoryScope.Create();
            string stdinFile = Path.Combine(tempDirectory.DirectoryPath, "stdin-helper.lua");
            await File.WriteAllTextAsync(stdinFile, "stdin data").ConfigureAwait(false);
            string missingWorkingDirectory = CreateMissingDirectoryPath("stdin-helper-working-");
            string missingBaseDirectory = CreateMissingDirectoryPath("stdin-helper-base-");

            string resolved = TapStdinHelper.ResolveInputPathForTests(
                "stdin-helper.lua",
                workingDirectory: missingWorkingDirectory,
                testDirectory: string.Empty,
                baseDirectory: missingBaseDirectory,
                runtimeDirectory: tempDirectory.DirectoryPath
            );

            await Assert.That(resolved).IsEqualTo(Path.GetFullPath(stdinFile));
        }

        private static string CreateMissingDirectoryPath(string namePrefix)
        {
            string path;
            using (TempDirectoryScope scope = TempDirectoryScope.Create(namePrefix: namePrefix))
            {
                path = scope.DirectoryPath;
            }

            return path;
        }
    }
}
