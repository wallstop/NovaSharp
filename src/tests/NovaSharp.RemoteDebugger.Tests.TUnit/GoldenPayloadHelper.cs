namespace NovaSharp.RemoteDebugger.Tests.TUnit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text.Json;

    /// <summary>
    /// Provides utilities for loading and comparing DAP protocol payloads against golden files.
    /// </summary>
    internal static class GoldenPayloadHelper
    {
        private static readonly JsonSerializerOptions CompareOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
        };

        /// <summary>
        /// Loads a golden JSON payload from the embedded GoldenPayloads directory.
        /// </summary>
        /// <param name="fileName">The golden file name (e.g., "initialize-response.json").</param>
        /// <returns>The parsed JsonDocument.</returns>
        public static JsonDocument LoadGoldenFile(string fileName)
        {
            string basePath = GetGoldenPayloadsDirectory();
            string fullPath = Path.Combine(basePath, fileName);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Golden payload file not found: {fullPath}");
            }

            string content = File.ReadAllText(fullPath);
            return JsonDocument.Parse(content);
        }

        /// <summary>
        /// Compares two JSON elements for semantic equality, ignoring property order and
        /// normalizing case-insensitive property names.
        /// </summary>
        /// <param name="expected">The expected (golden) element.</param>
        /// <param name="actual">The actual element from the test.</param>
        /// <param name="ignoredProperties">Properties to skip during comparison (e.g., sequence numbers).</param>
        /// <returns>A list of differences found, empty if elements match.</returns>
        public static IReadOnlyList<string> CompareJson(
            JsonElement expected,
            JsonElement actual,
            IReadOnlySet<string> ignoredProperties = null
        )
        {
            List<string> differences = new();
            ignoredProperties ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CompareJsonRecursive(expected, actual, "$", differences, ignoredProperties);
            return differences;
        }

        /// <summary>
        /// Extracts a normalized representation of a JSON element for comparison,
        /// stripping volatile fields like sequence numbers.
        /// </summary>
        public static JsonElement NormalizeForComparison(
            JsonElement element,
            IReadOnlySet<string> ignoredProperties = null
        )
        {
            ignoredProperties ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string normalized = NormalizeElement(element, ignoredProperties);
            using JsonDocument doc = JsonDocument.Parse(normalized);
            return doc.RootElement.Clone();
        }

        private static string NormalizeElement(
            JsonElement element,
            IReadOnlySet<string> ignoredProperties
        )
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    Dictionary<string, object> obj = new(StringComparer.OrdinalIgnoreCase);
                    foreach (JsonProperty prop in element.EnumerateObject())
                    {
                        if (ignoredProperties.Contains(prop.Name))
                        {
                            continue;
                        }
                        obj[prop.Name] = JsonSerializer.Deserialize<object>(
                            NormalizeElement(prop.Value, ignoredProperties),
                            CompareOptions
                        );
                    }
                    return JsonSerializer.Serialize(obj, CompareOptions);

                case JsonValueKind.Array:
                    List<object> arr = new();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        arr.Add(
                            JsonSerializer.Deserialize<object>(
                                NormalizeElement(item, ignoredProperties),
                                CompareOptions
                            )
                        );
                    }
                    return JsonSerializer.Serialize(arr, CompareOptions);

                default:
                    return element.GetRawText();
            }
        }

        private static void CompareJsonRecursive(
            JsonElement expected,
            JsonElement actual,
            string path,
            List<string> differences,
            IReadOnlySet<string> ignoredProperties
        )
        {
            if (expected.ValueKind != actual.ValueKind)
            {
                differences.Add(
                    $"{path}: Expected {expected.ValueKind} but got {actual.ValueKind}"
                );
                return;
            }

            switch (expected.ValueKind)
            {
                case JsonValueKind.Object:
                    CompareObjects(expected, actual, path, differences, ignoredProperties);
                    break;

                case JsonValueKind.Array:
                    CompareArrays(expected, actual, path, differences, ignoredProperties);
                    break;

                case JsonValueKind.String:
                    string expectedStr = expected.GetString();
                    string actualStr = actual.GetString();
                    if (!string.Equals(expectedStr, actualStr, StringComparison.Ordinal))
                    {
                        differences.Add(
                            $"{path}: Expected string \"{expectedStr}\" but got \"{actualStr}\""
                        );
                    }
                    break;

                case JsonValueKind.Number:
                    if (expected.TryGetInt64(out long expectedLong))
                    {
                        if (!actual.TryGetInt64(out long actualLong) || expectedLong != actualLong)
                        {
                            differences.Add(
                                $"{path}: Expected number {expectedLong} but got {actual.GetRawText()}"
                            );
                        }
                    }
                    else if (expected.TryGetDouble(out double expectedDouble))
                    {
                        if (
                            !actual.TryGetDouble(out double actualDouble)
                            || Math.Abs(expectedDouble - actualDouble) > 1e-10
                        )
                        {
                            differences.Add(
                                $"{path}: Expected number {expectedDouble} but got {actual.GetRawText()}"
                            );
                        }
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (expected.GetBoolean() != actual.GetBoolean())
                    {
                        differences.Add(
                            $"{path}: Expected {expected.GetBoolean()} but got {actual.GetBoolean()}"
                        );
                    }
                    break;

                case JsonValueKind.Null:
                    // Both are null, no difference
                    break;
            }
        }

        private static void CompareObjects(
            JsonElement expected,
            JsonElement actual,
            string path,
            List<string> differences,
            IReadOnlySet<string> ignoredProperties
        )
        {
            Dictionary<string, JsonElement> actualProps = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty prop in actual.EnumerateObject())
            {
                actualProps[prop.Name] = prop.Value;
            }

            foreach (JsonProperty expectedProp in expected.EnumerateObject())
            {
                if (ignoredProperties.Contains(expectedProp.Name))
                {
                    continue;
                }

                string childPath = $"{path}.{expectedProp.Name}";

                if (
                    !TryGetPropertyCaseInsensitive(
                        actualProps,
                        expectedProp.Name,
                        out JsonElement actualValue
                    )
                )
                {
                    differences.Add($"{childPath}: Missing property in actual");
                    continue;
                }

                CompareJsonRecursive(
                    expectedProp.Value,
                    actualValue,
                    childPath,
                    differences,
                    ignoredProperties
                );
            }
        }

        private static void CompareArrays(
            JsonElement expected,
            JsonElement actual,
            string path,
            List<string> differences,
            IReadOnlySet<string> ignoredProperties
        )
        {
            JsonElement[] expectedItems = ToArray(expected);
            JsonElement[] actualItems = ToArray(actual);

            if (expectedItems.Length != actualItems.Length)
            {
                differences.Add(
                    $"{path}: Expected array length {expectedItems.Length} but got {actualItems.Length}"
                );
                return;
            }

            for (int i = 0; i < expectedItems.Length; i++)
            {
                CompareJsonRecursive(
                    expectedItems[i],
                    actualItems[i],
                    $"{path}[{i}]",
                    differences,
                    ignoredProperties
                );
            }
        }

        private static JsonElement[] ToArray(JsonElement arrayElement)
        {
            List<JsonElement> items = new();
            foreach (JsonElement item in arrayElement.EnumerateArray())
            {
                items.Add(item);
            }
            return items.ToArray();
        }

        private static bool TryGetPropertyCaseInsensitive(
            Dictionary<string, JsonElement> props,
            string propertyName,
            out JsonElement value
        )
        {
            // Try exact match first
            if (props.TryGetValue(propertyName, out value))
            {
                return true;
            }

            // Try case-insensitive
            foreach (KeyValuePair<string, JsonElement> kvp in props)
            {
                if (string.Equals(kvp.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static string GetGoldenPayloadsDirectory()
        {
            // Navigate from the executing assembly location to find GoldenPayloads
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string projectDir = assemblyDir;

            // Walk up to find the project directory (contains .csproj)
            while (!string.IsNullOrEmpty(projectDir))
            {
                string goldenDir = Path.Combine(projectDir, "GoldenPayloads");
                if (Directory.Exists(goldenDir))
                {
                    return goldenDir;
                }

                // Also check for the test project specific path
                string[] csprojFiles = Directory.GetFiles(projectDir, "*.csproj");
                if (csprojFiles.Length > 0)
                {
                    goldenDir = Path.Combine(projectDir, "GoldenPayloads");
                    if (Directory.Exists(goldenDir))
                    {
                        return goldenDir;
                    }
                }

                projectDir = Path.GetDirectoryName(projectDir);
            }

            // Fallback: try relative to the workspace root
            string workspaceRoot = FindWorkspaceRoot(assemblyDir);
            if (!string.IsNullOrEmpty(workspaceRoot))
            {
                string fallbackPath = Path.Combine(
                    workspaceRoot,
                    "src",
                    "tests",
                    "NovaSharp.RemoteDebugger.Tests.TUnit",
                    "GoldenPayloads"
                );
                if (Directory.Exists(fallbackPath))
                {
                    return fallbackPath;
                }
            }

            throw new DirectoryNotFoundException(
                "Could not locate GoldenPayloads directory. Ensure the directory exists in the test project."
            );
        }

        private static string FindWorkspaceRoot(string startDir)
        {
            string current = startDir;
            while (!string.IsNullOrEmpty(current))
            {
                if (
                    File.Exists(Path.Combine(current, "NovaSharp.sln"))
                    || File.Exists(Path.Combine(current, "global.json"))
                )
                {
                    return current;
                }
                current = Path.GetDirectoryName(current);
            }
            return null;
        }
    }
}
