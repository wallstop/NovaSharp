namespace WallstopStudios.NovaSharp.Interpreter.Modding
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;

    /// <summary>
    /// Represents the metadata stored in a NovaSharp mod manifest (JSON).
    /// </summary>
    public sealed class ModManifest
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        public ModManifest(string name, string version, LuaCompatibilityVersion? luaCompatibility)
        {
            Name = name;
            Version = version;
            LuaCompatibility = luaCompatibility;
        }

        /// <summary>
        /// Gets the declared mod name (may be null/empty if not provided by the manifest).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the declared mod version (may be null/empty if not provided by the manifest).
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets the requested Lua compatibility version declared in the manifest, if any.
        /// </summary>
        public LuaCompatibilityVersion? LuaCompatibility { get; }

        /// <summary>
        /// Parses a manifest from a JSON string.
        /// </summary>
        public static ModManifest Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Manifest JSON cannot be empty.", nameof(json));
            }

            ManifestModel model =
                JsonSerializer.Deserialize<ManifestModel>(json, SerializerOptions)
                ?? throw new InvalidDataException("Manifest JSON resolved to an empty document.");

            return FromModel(model);
        }

        /// <summary>
        /// Parses a manifest from a stream containing UTF-8 JSON.
        /// </summary>
        public static ModManifest Load(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ManifestModel model =
                JsonSerializer.Deserialize<ManifestModel>(stream, SerializerOptions)
                ?? throw new InvalidDataException("Manifest JSON resolved to an empty document.");

            return FromModel(model);
        }

        /// <summary>
        /// Attempts to parse a manifest from a JSON string, returning false when parsing fails.
        /// </summary>
        public static bool TryParse(string json, out ModManifest manifest, out string error)
        {
            try
            {
                manifest = Parse(json);
                error = null;
                return true;
            }
            catch (Exception ex)
                when (ex is InvalidDataException || ex is JsonException || ex is ArgumentException)
            {
                manifest = null;
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Applies the manifest's requested Lua compatibility version to the provided <see cref="ScriptOptions"/>.
        /// When the manifest targets a newer version than the host supports, the optional warning sink is notified.
        /// </summary>
        /// <param name="options">The script options the manifest should configure.</param>
        /// <param name="hostCompatibility">
        /// The maximum Lua version supported by the host. Defaults to <see cref="Script.GlobalOptions"/> when omitted.
        /// </param>
        /// <param name="warningSink">
        /// An optional callback that receives a warning message when the requested compatibility version
        /// is newer than the host supports.
        /// </param>
        public void ApplyCompatibility(
            ScriptOptions options,
            LuaCompatibilityVersion? hostCompatibility = null,
            Action<string> warningSink = null
        )
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (!LuaCompatibility.HasValue)
            {
                return;
            }

            LuaCompatibilityVersion requested = Normalize(LuaCompatibility.Value);
            options.CompatibilityVersion = LuaCompatibility.Value;

            LuaCompatibilityVersion effectiveHost = Normalize(
                hostCompatibility ?? Script.GlobalOptions.CompatibilityVersion
            );

            if (IsNewerThan(requested, effectiveHost))
            {
                warningSink?.Invoke(
                    BuildCompatibilityWarning(
                        requested,
                        effectiveHost,
                        string.IsNullOrWhiteSpace(Name) ? "mod" : $"mod \"{Name}\""
                    )
                );
            }
        }

        private static ModManifest FromModel(ManifestModel model)
        {
            LuaCompatibilityVersion? compatibility = ParseCompatibility(model.LuaCompatibility);
            return new ModManifest(model.Name, model.Version, compatibility);
        }

        private static LuaCompatibilityVersion? ParseCompatibility(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string candidate = raw.Trim();

            if (Enum.TryParse(candidate, ignoreCase: true, out LuaCompatibilityVersion parsed))
            {
                return parsed;
            }

            string digitsOnly = ExtractDigits(candidate);

            if (digitsOnly.Length == 0)
            {
                throw new InvalidDataException(
                    $"Unable to parse luaCompatibility value \"{raw}\". Expected examples: \"Lua54\", \"5.4\", \"latest\"."
                );
            }

            string enumCandidate = "Lua" + digitsOnly;

            if (Enum.TryParse(enumCandidate, ignoreCase: true, out parsed))
            {
                return parsed;
            }

            throw new InvalidDataException(
                $"Unable to parse luaCompatibility value \"{raw}\". Expected examples: \"Lua54\", \"5.4\", \"latest\"."
            );
        }

        private static string ExtractDigits(string value)
        {
            Span<char> buffer = stackalloc char[value.Length];
            int idx = 0;

            foreach (char c in value)
            {
                if (char.IsDigit(c))
                {
                    buffer[idx++] = c;
                }
            }

            return new string(buffer.Slice(0, idx));
        }

        private static LuaCompatibilityVersion Normalize(LuaCompatibilityVersion version)
        {
            // Use ResolveForHighest for forward-compatibility checks in manifests
            return LuaVersionDefaults.ResolveForHighest(version);
        }

        private static bool IsNewerThan(
            LuaCompatibilityVersion requested,
            LuaCompatibilityVersion host
        )
        {
            return (int)requested > (int)host;
        }

        private static string BuildCompatibilityWarning(
            LuaCompatibilityVersion requested,
            LuaCompatibilityVersion host,
            string manifestLabel
        )
        {
            string requestedDisplay = LuaCompatibilityProfile.ForVersion(requested).DisplayName;
            string hostDisplay = LuaCompatibilityProfile.ForVersion(host).DisplayName;

            return $"{manifestLabel} targets {requestedDisplay}, but the host default is {hostDisplay}.";
        }

        private sealed class ManifestModel
        {
            [JsonPropertyName("name")]
            /// <summary>
            /// Gets or sets the optional mod name parsed directly from JSON.
            /// </summary>
            public string Name { get; set; }

            [JsonPropertyName("version")]
            /// <summary>
            /// Gets or sets the optional mod version parsed directly from JSON.
            /// </summary>
            public string Version { get; set; }

            [JsonPropertyName("luaCompatibility")]
            /// <summary>
            /// Gets or sets the raw compatibility string (e.g., "Lua54", "5.4", "latest").
            /// </summary>
            public string LuaCompatibility { get; set; }
        }
    }
}
