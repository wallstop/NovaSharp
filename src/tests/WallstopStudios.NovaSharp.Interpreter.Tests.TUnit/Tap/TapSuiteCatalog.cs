namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Tap
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;

    internal static class TapSuiteCatalog
    {
        public static IEnumerable<TapSuiteDefinition> GetTestMoreSuites()
        {
            return EnumerateSuites("TestMore", GetTestMoreCompatibilityOverride);
        }

        private static IEnumerable<TapSuiteDefinition> EnumerateSuites(
            string relativeDirectory,
            Func<string, LuaCompatibilityVersion?> compatibilityFactory
        )
        {
            string directory = Path.Combine(AppContext.BaseDirectory, relativeDirectory);
            if (!Directory.Exists(directory))
            {
                yield break;
            }

            IEnumerable<string> suites = Directory
                .EnumerateFiles(directory, "*.t", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

            foreach (string suitePath in suites)
            {
                string relativePath = Path.GetRelativePath(directory, suitePath)
                    .Replace(Path.DirectorySeparatorChar, '/');
                relativePath = $"{relativeDirectory}/{relativePath}";
                LuaCompatibilityVersion? compatibility = compatibilityFactory(relativePath);
                yield return new TapSuiteDefinition(relativePath, compatibility);
            }
        }

        private static LuaCompatibilityVersion? GetTestMoreCompatibilityOverride(string path)
        {
            if (IsBit32Suite(path))
            {
                return LuaCompatibilityVersion.Lua52;
            }

            return null;
        }

        private static bool IsBit32Suite(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return string.Equals(
                    path,
                    "TestMore/StandardLibrary/307-bit.t",
                    StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    path,
                    "TestMore/StandardLibrary/lua-bitwise-library.t",
                    StringComparison.OrdinalIgnoreCase
                );
        }
    }

    internal sealed class TapSuiteDefinition
    {
        public TapSuiteDefinition(string relativePath, LuaCompatibilityVersion? compatibility)
        {
            RelativePath = relativePath;
            CompatibilityVersion = compatibility;
        }

        public string RelativePath { get; }

        public LuaCompatibilityVersion? CompatibilityVersion { get; }
    }
}
