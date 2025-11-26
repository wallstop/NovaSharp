namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modding;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ModManifestCompatibilityTests
    {
        [Test]
        public void TryApplyFromScriptPathAppliesManifestAndLogsInfo()
        {
            string tempDir = CreateTempDirectory();
            string scriptPath = Path.Combine(tempDir, "main.lua");
            File.WriteAllText(scriptPath, "return 1");

            File.WriteAllText(
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

            Assert.Multiple(() =>
            {
                Assert.That(applied, Is.True);
                Assert.That(
                    options.CompatibilityVersion,
                    Is.EqualTo(LuaCompatibilityVersion.Lua53)
                );
                Assert.That(info, Has.Count.EqualTo(1));
                Assert.That(info[0], Does.Contain("Lua 5.3"));
                Assert.That(warnings, Is.Empty);
            });
        }

        [Test]
        public void TryApplyFromDirectoryHandlesInvalidManifest()
        {
            string tempDir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(tempDir, "mod.json"), "{ this is: not json");

            ScriptOptions options = new(Script.DefaultOptions);
            List<string> warnings = new();

            bool applied = ModManifestCompatibility.TryApplyFromDirectory(
                tempDir,
                options,
                LuaCompatibilityVersion.Lua55,
                infoSink: null,
                warningSink: warnings.Add
            );

            Assert.Multiple(() =>
            {
                Assert.That(applied, Is.False);
                Assert.That(warnings, Has.Count.EqualTo(1));
                Assert.That(warnings[0], Does.Contain("Failed to load"));
            });
        }

        [Test]
        public void CreateScriptFromDirectoryAppliesCompatibilityBeforeInstantiation()
        {
            string tempDir = CreateTempDirectory();
            File.WriteAllText(
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

            Assert.Multiple(() =>
            {
                Assert.That(script.CompatibilityVersion, Is.EqualTo(LuaCompatibilityVersion.Lua52));
                Assert.That(info, Is.Not.Empty);
                Assert.That(info[0], Does.Contain("Lua 5.2"));
            });
        }

        [Test]
        public void CustomFileSystemCanProvideManifest()
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

            Assert.Multiple(() =>
            {
                Assert.That(applied, Is.True);
                Assert.That(
                    options.CompatibilityVersion,
                    Is.EqualTo(LuaCompatibilityVersion.Lua53)
                );
                Assert.That(fileSystem.OpenReadCallCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void TryApplyFromScriptPathReturnsFalseWhenPathMissing()
        {
            ScriptOptions options = new(Script.DefaultOptions);
            bool result = ModManifestCompatibility.TryApplyFromScriptPath(
                null,
                options,
                infoSink: null,
                warningSink: null
            );

            Assert.That(result, Is.False);
        }

        [Test]
        public void TryApplyFromDirectoryReturnsFalseWhenManifestMissing()
        {
            string tempDir = CreateTempDirectory();
            ScriptOptions options = new(Script.DefaultOptions);

            bool applied = ModManifestCompatibility.TryApplyFromDirectory(tempDir, options);
            Assert.That(applied, Is.False);
        }

        [Test]
        public void TryApplyFromDirectoryDoesNotEmitInfoWhenCompatibilityMissing()
        {
            string tempDir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(tempDir, "mod.json"), "{ \"name\": \"sample\" }");

            ScriptOptions options = new(Script.DefaultOptions);
            List<string> info = new();

            bool applied = ModManifestCompatibility.TryApplyFromDirectory(
                tempDir,
                options,
                infoSink: info.Add,
                warningSink: null
            );

            Assert.Multiple(() =>
            {
                Assert.That(applied, Is.True);
                Assert.That(info, Is.Empty);
            });
        }

        [Test]
        public void TryApplyFromScriptPathResolvesDirectoriesDirectly()
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

            Assert.Multiple(() =>
            {
                Assert.That(applied, Is.True);
                Assert.That(
                    options.CompatibilityVersion,
                    Is.EqualTo(LuaCompatibilityVersion.Lua54)
                );
            });
        }

        [Test]
        public void TryApplyFromScriptPathHandlesFullPathExceptions()
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

            Assert.That(applied, Is.False);
            Assert.That(fileSystem.GetFullPathAttempts, Is.EqualTo(1));
        }

        [Test]
        public void TryApplyFromDirectoryThrowsWhenOptionsNull()
        {
            Assert.That(
                () => ModManifestCompatibility.TryApplyFromDirectory("mods", null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options")
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
