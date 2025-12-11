namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Provides helper methods for loading and running Lua fixture files in tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This helper supports the file-based test authoring pattern where Lua tests are written
    /// as standalone <c>.lua</c> files in <c>LuaFixtures/&lt;TestClass&gt;/&lt;TestMethod&gt;.lua</c>
    /// and loaded via <see cref="Script.DoFile"/> instead of inline <see cref="Script.DoString"/>.
    /// </para>
    /// <para>
    /// Each fixture file can include metadata headers:
    /// <list type="bullet">
    ///   <item><c>-- @lua-versions: 5.1+</c> - Compatible Lua versions</item>
    ///   <item><c>-- @novasharp-only: true</c> - Uses NovaSharp-specific features</item>
    ///   <item><c>-- @expects-error: true</c> - Expected to throw</item>
    ///   <item><c>-- @expected-output: ...</c> - Expected printed output</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class LuaFixtureHelper
    {
        private static readonly string FixturesBasePath = FindFixturesBasePath();

        private static string FindFixturesBasePath()
        {
            // Locate the LuaFixtures directory relative to the assembly location
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

            // Walk up to find src/tests directory
            DirectoryInfo directory = new(assemblyDirectory);
            while (
                directory != null
                && !string.Equals(directory.Name, "tests", StringComparison.OrdinalIgnoreCase)
            )
            {
                directory = directory.Parent;
            }

            if (directory != null)
            {
                string fixturesPath = Path.Combine(
                    directory.FullName,
                    "WallstopStudios.NovaSharp.Interpreter.Tests",
                    "LuaFixtures"
                );
                if (Directory.Exists(fixturesPath))
                {
                    return fixturesPath;
                }
            }

            // Fallback: try relative from working directory
            string fallbackPath = Path.GetFullPath(
                "../WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures"
            );
            if (Directory.Exists(fallbackPath))
            {
                return fallbackPath;
            }

            // Development fallback: check from repository root
            string repoRoot = FindRepositoryRoot();
            if (repoRoot != null)
            {
                string devPath = Path.Combine(
                    repoRoot,
                    "src",
                    "tests",
                    "WallstopStudios.NovaSharp.Interpreter.Tests",
                    "LuaFixtures"
                );
                if (Directory.Exists(devPath))
                {
                    return devPath;
                }
            }

            return null;
        }

        private readonly string _testClassName;
        private readonly string _testMethodName;

        /// <summary>
        /// Creates a fixture helper for the specified test class and method.
        /// </summary>
        /// <param name="testClassName">The name of the test class (used as folder name).</param>
        /// <param name="testMethodName">The name of the test method (used as file name without extension).</param>
        public LuaFixtureHelper(string testClassName, string testMethodName)
        {
            if (string.IsNullOrEmpty(testClassName))
            {
                throw new ArgumentException(
                    "Test class name must be provided.",
                    nameof(testClassName)
                );
            }
            if (string.IsNullOrEmpty(testMethodName))
            {
                throw new ArgumentException(
                    "Test method name must be provided.",
                    nameof(testMethodName)
                );
            }

            _testClassName = testClassName;
            _testMethodName = testMethodName;
        }

        /// <summary>
        /// Creates a fixture helper for the calling test method.
        /// </summary>
        /// <param name="testMethodName">
        /// Automatically captured from the calling method name. Do not pass explicitly.
        /// </param>
        /// <returns>A new <see cref="LuaFixtureHelper"/> instance.</returns>
        public static LuaFixtureHelper ForCallingTest<TTestClass>(
            [CallerMemberName] string testMethodName = null
        )
        {
            string className = typeof(TTestClass).Name;
            return new LuaFixtureHelper(className, testMethodName);
        }

        /// <summary>
        /// Gets the full path to the fixture file.
        /// </summary>
        /// <param name="suffix">Optional suffix for fixtures with multiple snippets (e.g., "_1", "_2").</param>
        /// <returns>The full path to the fixture file.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the fixtures directory cannot be located.</exception>
        public string GetFixturePath(string suffix = null)
        {
            if (FixturesBasePath == null)
            {
                throw new InvalidOperationException(
                    "Could not locate the LuaFixtures directory. Ensure the test is running from the correct location."
                );
            }

            string fileName = string.IsNullOrEmpty(suffix)
                ? $"{_testMethodName}.lua"
                : $"{_testMethodName}{suffix}.lua";

            return Path.Combine(FixturesBasePath, _testClassName, fileName);
        }

        /// <summary>
        /// Checks whether the fixture file exists.
        /// </summary>
        /// <param name="suffix">Optional suffix for fixtures with multiple snippets.</param>
        /// <returns><c>true</c> if the fixture file exists; otherwise, <c>false</c>.</returns>
        public bool FixtureExists(string suffix = null)
        {
            if (FixturesBasePath == null)
            {
                return false;
            }

            return File.Exists(GetFixturePath(suffix));
        }

        /// <summary>
        /// Loads the fixture file content.
        /// </summary>
        /// <param name="suffix">Optional suffix for fixtures with multiple snippets.</param>
        /// <returns>The content of the fixture file.</returns>
        public string LoadFixtureContent(string suffix = null)
        {
            string path = GetFixturePath(suffix);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Lua fixture file not found: {path}", path);
            }

            return File.ReadAllText(path, Encoding.UTF8);
        }

        /// <summary>
        /// Parses metadata from the fixture file headers.
        /// </summary>
        /// <param name="suffix">Optional suffix for fixtures with multiple snippets.</param>
        /// <returns>The parsed metadata.</returns>
        public LuaFixtureMetadata LoadMetadata(string suffix = null)
        {
            string content = LoadFixtureContent(suffix);
            return LuaFixtureMetadata.Parse(content);
        }

        /// <summary>
        /// Runs the fixture file with the provided script.
        /// </summary>
        /// <param name="script">The script to execute the fixture with.</param>
        /// <param name="suffix">Optional suffix for fixtures with multiple snippets.</param>
        /// <returns>The result of executing the fixture.</returns>
        public DynValue RunFixture(Script script, string suffix = null)
        {
            ArgumentNullException.ThrowIfNull(script);

            string path = GetFixturePath(suffix);
            return script.DoFile(path);
        }

        /// <summary>
        /// Runs the fixture file with a new default script.
        /// </summary>
        /// <param name="suffix">Optional suffix for fixtures with multiple snippets.</param>
        /// <returns>The result of executing the fixture.</returns>
        public DynValue RunFixture(string suffix = null)
        {
            return RunFixture(new Script(), suffix);
        }

        /// <summary>
        /// Enumerates all fixture files for this test class.
        /// </summary>
        /// <returns>An enumerable of fixture file paths.</returns>
        public IEnumerable<string> EnumerateFixtures()
        {
            if (FixturesBasePath == null)
            {
                yield break;
            }

            string classDirectory = Path.Combine(FixturesBasePath, _testClassName);
            if (!Directory.Exists(classDirectory))
            {
                yield break;
            }

            string pattern = $"{_testMethodName}*.lua";
            foreach (string file in Directory.EnumerateFiles(classDirectory, pattern))
            {
                yield return file;
            }
        }

        private static string FindRepositoryRoot()
        {
            string current = Directory.GetCurrentDirectory();
            DirectoryInfo directory = new(current);

            while (directory != null)
            {
                if (
                    File.Exists(Path.Combine(directory.FullName, "NovaSharp.sln"))
                    || File.Exists(Path.Combine(directory.FullName, "PLAN.md"))
                )
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }

            return null;
        }
    }

    /// <summary>
    /// Represents metadata parsed from Lua fixture file headers.
    /// </summary>
    public sealed class LuaFixtureMetadata
    {
        private static readonly Regex HeaderPattern = new(
            @"^--\s*@(?<key>[\w-]+):\s*(?<value>.*)$",
            RegexOptions.Compiled | RegexOptions.Multiline
        );

        private LuaFixtureMetadata()
        {
            LuaVersions = Array.Empty<string>();
        }

        /// <summary>
        /// Gets the compatible Lua versions (e.g., ["5.1", "5.2", "5.3", "5.4"]).
        /// </summary>
        public IReadOnlyList<string> LuaVersions { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the fixture uses NovaSharp-only features.
        /// </summary>
        public bool NovaSharpOnly { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the fixture is expected to throw an error.
        /// </summary>
        public bool ExpectsError { get; private set; }

        /// <summary>
        /// Gets the source file path (for traceability).
        /// </summary>
        public string SourcePath { get; private set; }

        /// <summary>
        /// Gets the test name (for traceability).
        /// </summary>
        public string TestName { get; private set; }

        /// <summary>
        /// Gets optional expected output for assertion.
        /// </summary>
        public string ExpectedOutput { get; private set; }

        /// <summary>
        /// Parses metadata from a Lua fixture file content.
        /// </summary>
        /// <param name="content">The fixture file content.</param>
        /// <returns>The parsed metadata.</returns>
        public static LuaFixtureMetadata Parse(string content)
        {
            LuaFixtureMetadata metadata = new();

            if (string.IsNullOrEmpty(content))
            {
                return metadata;
            }

            MatchCollection matches = HeaderPattern.Matches(content);
            List<string> versions = new();

            foreach (Match match in matches)
            {
                string key = match.Groups["key"].Value.ToUpperInvariant();
                string value = match.Groups["value"].Value.Trim();

                switch (key)
                {
                    case "LUA-VERSIONS":
                        versions.AddRange(ParseVersions(value));
                        break;
                    case "NOVASHARP-ONLY":
                        metadata.NovaSharpOnly = ParseBool(value);
                        break;
                    case "EXPECTS-ERROR":
                        metadata.ExpectsError = ParseBool(value);
                        break;
                    case "SOURCE":
                        metadata.SourcePath = value;
                        break;
                    case "TEST":
                        metadata.TestName = value;
                        break;
                    case "EXPECTED-OUTPUT":
                        metadata.ExpectedOutput = value;
                        break;
                }
            }

            metadata.LuaVersions =
                versions.Count > 0 ? versions.ToArray() : new[] { "5.1", "5.2", "5.3", "5.4" };
            return metadata;
        }

        private static IEnumerable<string> ParseVersions(string value)
        {
            // Handle "5.1+" syntax (all versions from 5.1 onwards)
            if (value.EndsWith('+'))
            {
                string baseVersion = value.TrimEnd('+');
                string[] allVersions = { "5.1", "5.2", "5.3", "5.4" };
                bool found = false;
                foreach (string version in allVersions)
                {
                    if (string.Equals(version, baseVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                    }
                    if (found)
                    {
                        yield return version;
                    }
                }
                yield break;
            }

            // Handle comma-separated versions
            foreach (string part in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                yield return part.Trim();
            }
        }

        private static bool ParseBool(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "1", StringComparison.Ordinal);
        }
    }
}
