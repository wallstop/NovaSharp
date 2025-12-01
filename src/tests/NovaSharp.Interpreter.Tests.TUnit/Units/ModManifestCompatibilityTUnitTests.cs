#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modding;
    using NovaSharp.Interpreter.Modules;

    public sealed class ModManifestCompatibilityTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TryApplyFromScriptPathAppliesManifestAndLogsInfo()
        {
            string tempDir = CreateTempDirectory();
            string scriptPath = Path.Combine(tempDir, "main.lua");
            await File.WriteAllTextAsync(scriptPath, "return 1");

            await File.WriteAllTextAsync(
                Path.Combine(tempDir, "mod.json"),
                "{ \"luaCompatibility\": \"Lua53\" }"
            );

            ScriptOptions options = new(Script.DefaultOptions);
            List<string> info = new();
            List<string> warnings = new();

            bool applied = ModManifestCompatibility.TryApplyFromScriptPath(
                scriptPath,
                options,
                LuaCompatibilityVersion.Lua55,
                info.Add,
                warnings.Add
            );

            await Assert.That(applied).IsTrue();
            await Assert
                .That(options.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Lua53);
            await Assert.That(info.Count).IsEqualTo(1);
            await Assert.That(info[0]).Contains("Lua 5.3");
            await Assert.That(warnings.Count).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task TryApplyFromDirectoryHandlesInvalidManifest()
        {
            string tempDir = CreateTempDirectory();
            await File.WriteAllTextAsync(Path.Combine(tempDir, "mod.json"), "{ this is: not json");

            ScriptOptions options = new(Script.DefaultOptions);
            List<string> warnings = new();

            bool applied = ModManifestCompatibility.TryApplyFromDirectory(
                tempDir,
                options,
                LuaCompatibilityVersion.Lua55,
                infoSink: null,
                warningSink: warnings.Add
            );

            await Assert.That(applied).IsFalse();
            await Assert.That(warnings.Count).IsEqualTo(1);
            await Assert.That(warnings[0]).Contains("Failed to load");
        }

        [global::TUnit.Core.Test]
        public async Task CreateScriptFromDirectoryAppliesCompatibilityBeforeInstantiation()
        {
            string tempDir = CreateTempDirectory();
            await File.WriteAllTextAsync(
                Path.Combine(tempDir, "mod.json"),
                "{ \"name\": \"mod\", \"luaCompatibility\": \"Lua52\" }"
            );

            List<string> info = new();

            Script script = ModManifestCompatibility.CreateScriptFromDirectory(
                tempDir,
                CoreModules.Basic,
                baseOptions: null,
                infoSink: info.Add,
                warningSink: null
            );

            await Assert.That(script.CompatibilityVersion).IsEqualTo(LuaCompatibilityVersion.Lua52);
            await Assert.That(info.Count).IsGreaterThan(0);
            await Assert.That(info[0]).Contains("Lua 5.2");
        }

        [global::TUnit.Core.Test]
        public async Task CustomFileSystemCanProvideManifest()
        {
            TestModFileSystem fileSystem = new();
            string directory = fileSystem.AddDirectory("mods/sample");
            fileSystem.AddFile(
                Path.Combine(directory, "mod.json"),
                "{ \"luaCompatibility\": \"Lua53\" }"
            );

            ScriptOptions options = new(Script.DefaultOptions);
            bool applied = ModManifestCompatibility.TryApplyFromDirectory(
                directory,
                options,
                LuaCompatibilityVersion.Lua55,
                infoSink: null,
                warningSink: null,
                fileSystem: fileSystem
            );

            await Assert.That(applied).IsTrue();
            await Assert
                .That(options.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Lua53);
            await Assert.That(fileSystem.OpenReadCallCount).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task TryApplyFromScriptPathReturnsFalseWhenPathMissing()
        {
            ScriptOptions options = new(Script.DefaultOptions);
            bool result = ModManifestCompatibility.TryApplyFromScriptPath(
                null,
                options,
                infoSink: null,
                warningSink: null
            );

            await Assert.That(result).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task TryApplyFromDirectoryReturnsFalseWhenManifestMissing()
        {
            string tempDir = CreateTempDirectory();
            ScriptOptions options = new(Script.DefaultOptions);

            bool applied = ModManifestCompatibility.TryApplyFromDirectory(tempDir, options);
            await Assert.That(applied).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task TryApplyFromDirectoryDoesNotEmitInfoWhenCompatibilityMissing()
        {
            string tempDir = CreateTempDirectory();
            await File.WriteAllTextAsync(
                Path.Combine(tempDir, "mod.json"),
                "{ \"name\": \"sample\" }"
            );

            ScriptOptions options = new(Script.DefaultOptions);
            List<string> info = new();

            bool applied = ModManifestCompatibility.TryApplyFromDirectory(
                tempDir,
                options,
                infoSink: info.Add,
                warningSink: null
            );

            await Assert.That(applied).IsTrue();
            await Assert.That(info.Count).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task TryApplyFromScriptPathResolvesDirectoriesDirectly()
        {
            TestModFileSystem fileSystem = new();
            string directory = fileSystem.AddDirectory("mods/dirscript");
            fileSystem.AddFile(
                Path.Combine(directory, "mod.json"),
                "{ \"luaCompatibility\": \"Lua54\" }"
            );

            ScriptOptions options = new(Script.DefaultOptions);
            bool applied = ModManifestCompatibility.TryApplyFromScriptPath(
                directory,
                options,
                infoSink: null,
                warningSink: null,
                fileSystem: fileSystem
            );

            await Assert.That(applied).IsTrue();
            await Assert
                .That(options.CompatibilityVersion)
                .IsEqualTo(LuaCompatibilityVersion.Lua54);
        }

        [global::TUnit.Core.Test]
        public async Task TryApplyFromScriptPathHandlesFullPathExceptions()
        {
            ThrowingFileSystem fileSystem = new();
            ScriptOptions options = new(Script.DefaultOptions);

            bool applied = ModManifestCompatibility.TryApplyFromScriptPath(
                "invalid::path",
                options,
                infoSink: null,
                warningSink: null,
                fileSystem: fileSystem
            );

            await Assert.That(applied).IsFalse();
            await Assert.That(fileSystem.GetFullPathAttempts).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public void TryApplyFromDirectoryThrowsWhenOptionsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ModManifestCompatibility.TryApplyFromDirectory("mods", null)
            );
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                $"novasharp_modcompat_{Guid.NewGuid():N}"
            );
            Directory.CreateDirectory(path);
            return path;
        }

        private sealed class TestModFileSystem : IModFileSystem
        {
            private readonly Dictionary<string, string> _files = new(
                StringComparer.OrdinalIgnoreCase
            );
            private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);

            internal int OpenReadCallCount { get; private set; }

            internal string AddDirectory(string path)
            {
                string normalized = Normalize(path);
                _directories.Add(normalized);
                return normalized;
            }

            internal void AddFile(string path, string contents)
            {
                string normalized = Normalize(path);
                string directory = Path.GetDirectoryName(normalized);
                if (!string.IsNullOrEmpty(directory))
                {
                    _directories.Add(directory);
                }

                _files[normalized] = contents;
            }

            public bool FileExists(string path)
            {
                return _files.ContainsKey(Normalize(path));
            }

            public bool DirectoryExists(string path)
            {
                return _directories.Contains(Normalize(path));
            }

            public Stream OpenRead(string path)
            {
                OpenReadCallCount++;
                string normalized = Normalize(path);
                return new MemoryStream(Encoding.UTF8.GetBytes(_files[normalized]));
            }

            public string GetFullPath(string path)
            {
                return Normalize(path);
            }

            public string GetDirectoryName(string path)
            {
                string normalized = Normalize(path);
                string directory = Path.GetDirectoryName(normalized);
                return string.IsNullOrEmpty(directory) ? null : directory;
            }

            private static string Normalize(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return path;
                }

                string replaced = path.Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);

                if (Path.IsPathRooted(replaced))
                {
                    return Path.GetFullPath(replaced);
                }

                string basePath = Path.Combine(Path.GetTempPath(), replaced);
                return Path.GetFullPath(basePath);
            }
        }

        private sealed class ThrowingFileSystem : IModFileSystem
        {
            public int GetFullPathAttempts { get; private set; }

            public bool FileExists(string path)
            {
                return false;
            }

            public bool DirectoryExists(string path)
            {
                return false;
            }

            public Stream OpenRead(string path)
            {
                throw new NotSupportedException();
            }

            public string GetFullPath(string path)
            {
                GetFullPathAttempts++;
                throw new ArgumentException("invalid path");
            }

            public string GetDirectoryName(string path)
            {
                return null;
            }
        }
    }
}
#pragma warning restore CA2007
