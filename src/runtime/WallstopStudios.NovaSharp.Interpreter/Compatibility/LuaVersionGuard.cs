namespace WallstopStudios.NovaSharp.Interpreter.Compatibility
{
    using System;
    using System.ComponentModel;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Provides version guard helpers for Lua standard library functions that were
    /// added, deprecated, or removed across different Lua versions.
    /// </summary>
    /// <remarks>
    /// Use these guards at the entry point of standard library functions to ensure
    /// correct version-dependent behavior. For functions that exist across all versions
    /// but with different semantics, use <see cref="LuaNumberHelpers"/> validators instead.
    /// </remarks>
    public static class LuaVersionGuard
    {
        /// <summary>
        /// Throws if the specified function is not available in the given Lua version.
        /// Use this for functions that were added in later Lua versions.
        /// </summary>
        /// <param name="version">The active script compatibility version.</param>
        /// <param name="minimumVersion">The minimum Lua version that supports this function.</param>
        /// <param name="functionName">The function name for the error message (e.g., "coroutine.close").</param>
        /// <exception cref="ScriptRuntimeException">
        /// Thrown when the function is not available in the specified Lua version.
        /// </exception>
        /// <example>
        /// <code>
        /// // coroutine.close is only available in Lua 5.4+
        /// LuaVersionGuard.ThrowIfUnavailable(script.CompatibilityVersion, LuaCompatibilityVersion.Lua54, "coroutine.close");
        /// </code>
        /// </example>
        public static void ThrowIfUnavailable(
            LuaCompatibilityVersion version,
            LuaCompatibilityVersion minimumVersion,
            string functionName
        )
        {
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);
            LuaCompatibilityVersion resolvedMin = LuaVersionDefaults.Resolve(minimumVersion);

            if (resolved < resolvedMin)
            {
                string versionName = GetVersionDisplayName(resolvedMin);
                string activeVersion = GetVersionDisplayName(resolved);
                throw new ScriptRuntimeException(
                    $"attempt to call a nil value (function '{functionName}' requires {versionName} or later, but script is running in {activeVersion} mode)"
                );
            }
        }

        /// <summary>
        /// Throws if the specified function has been removed/deprecated in the given Lua version.
        /// Use this for functions that were removed in later Lua versions.
        /// </summary>
        /// <param name="version">The active script compatibility version.</param>
        /// <param name="maximumVersion">The last Lua version that supports this function.</param>
        /// <param name="functionName">The function name for the error message (e.g., "setfenv").</param>
        /// <exception cref="ScriptRuntimeException">
        /// Thrown when the function has been removed in the specified Lua version.
        /// </exception>
        /// <example>
        /// <code>
        /// // setfenv was removed in Lua 5.2
        /// LuaVersionGuard.ThrowIfRemoved(script.CompatibilityVersion, LuaCompatibilityVersion.Lua51, "setfenv");
        /// </code>
        /// </example>
        public static void ThrowIfRemoved(
            LuaCompatibilityVersion version,
            LuaCompatibilityVersion maximumVersion,
            string functionName
        )
        {
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);
            LuaCompatibilityVersion resolvedMax = LuaVersionDefaults.Resolve(maximumVersion);

            if (resolved > resolvedMax)
            {
                string removedInVersion = GetNextVersionDisplayName(resolvedMax);
                string activeVersion = GetVersionDisplayName(resolved);
                throw new ScriptRuntimeException(
                    $"attempt to call a nil value (function '{functionName}' was removed in {removedInVersion}, but script is running in {activeVersion} mode)"
                );
            }
        }

        /// <summary>
        /// Checks if a function is available in the given Lua version without throwing.
        /// </summary>
        /// <param name="version">The active script compatibility version.</param>
        /// <param name="minimumVersion">The minimum Lua version that supports this function.</param>
        /// <returns><c>true</c> if the function is available; otherwise, <c>false</c>.</returns>
        public static bool IsAvailable(
            LuaCompatibilityVersion version,
            LuaCompatibilityVersion minimumVersion
        )
        {
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);
            LuaCompatibilityVersion resolvedMin = LuaVersionDefaults.Resolve(minimumVersion);
            return resolved >= resolvedMin;
        }

        /// <summary>
        /// Checks if a function has been removed in the given Lua version without throwing.
        /// </summary>
        /// <param name="version">The active script compatibility version.</param>
        /// <param name="maximumVersion">The last Lua version that supports this function.</param>
        /// <returns><c>true</c> if the function has been removed; otherwise, <c>false</c>.</returns>
        public static bool IsRemoved(
            LuaCompatibilityVersion version,
            LuaCompatibilityVersion maximumVersion
        )
        {
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);
            LuaCompatibilityVersion resolvedMax = LuaVersionDefaults.Resolve(maximumVersion);
            return resolved > resolvedMax;
        }

        /// <summary>
        /// Checks if a function is available in the specified version range.
        /// </summary>
        /// <param name="version">The active script compatibility version.</param>
        /// <param name="minimumVersion">The minimum Lua version that supports this function.</param>
        /// <param name="maximumVersion">The maximum Lua version that supports this function, or <c>null</c> if still available.</param>
        /// <returns><c>true</c> if the function is available in the version range; otherwise, <c>false</c>.</returns>
        public static bool IsAvailableInRange(
            LuaCompatibilityVersion version,
            LuaCompatibilityVersion minimumVersion,
            LuaCompatibilityVersion? maximumVersion
        )
        {
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);
            LuaCompatibilityVersion resolvedMin = LuaVersionDefaults.Resolve(minimumVersion);

            if (resolved < resolvedMin)
            {
                return false;
            }

            if (maximumVersion.HasValue)
            {
                LuaCompatibilityVersion resolvedMax = LuaVersionDefaults.Resolve(
                    maximumVersion.Value
                );
                if (resolved > resolvedMax)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Throws if the function is not available in the specified version range.
        /// Use this for functions that were added in one version and removed in another.
        /// </summary>
        /// <param name="version">The active script compatibility version.</param>
        /// <param name="minimumVersion">The minimum Lua version that supports this function.</param>
        /// <param name="maximumVersion">The last Lua version that supports this function.</param>
        /// <param name="functionName">The function name for error messages.</param>
        /// <exception cref="ScriptRuntimeException">
        /// Thrown when the function is not available in the specified Lua version.
        /// </exception>
        /// <example>
        /// <code>
        /// // bit32 library is only available in Lua 5.2
        /// LuaVersionGuard.ThrowIfOutsideRange(script.CompatibilityVersion, LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua52, "bit32.band");
        /// </code>
        /// </example>
        public static void ThrowIfOutsideRange(
            LuaCompatibilityVersion version,
            LuaCompatibilityVersion minimumVersion,
            LuaCompatibilityVersion maximumVersion,
            string functionName
        )
        {
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);
            LuaCompatibilityVersion resolvedMin = LuaVersionDefaults.Resolve(minimumVersion);
            LuaCompatibilityVersion resolvedMax = LuaVersionDefaults.Resolve(maximumVersion);

            if (resolved < resolvedMin)
            {
                string versionName = GetVersionDisplayName(resolvedMin);
                string activeVersion = GetVersionDisplayName(resolved);
                throw new ScriptRuntimeException(
                    $"attempt to call a nil value (function '{functionName}' requires {versionName} or later, but script is running in {activeVersion} mode)"
                );
            }

            if (resolved > resolvedMax)
            {
                string removedInVersion = GetNextVersionDisplayName(resolvedMax);
                string activeVersion = GetVersionDisplayName(resolved);
                throw new ScriptRuntimeException(
                    $"attempt to call a nil value (function '{functionName}' was removed in {removedInVersion}, but script is running in {activeVersion} mode)"
                );
            }
        }

        /// <summary>
        /// Gets the human-readable display name for a Lua version.
        /// </summary>
        internal static string GetVersionDisplayName(LuaCompatibilityVersion version)
        {
            return version switch
            {
                LuaCompatibilityVersion.Lua51 => "Lua 5.1",
                LuaCompatibilityVersion.Lua52 => "Lua 5.2",
                LuaCompatibilityVersion.Lua53 => "Lua 5.3",
                LuaCompatibilityVersion.Lua54 => "Lua 5.4",
                LuaCompatibilityVersion.Lua55 => "Lua 5.5",
                LuaCompatibilityVersion.Latest => "Lua 5.4",
                _ => throw new InvalidEnumArgumentException(
                    nameof(version),
                    (int)version,
                    typeof(LuaCompatibilityVersion)
                ),
            };
        }

        /// <summary>
        /// Gets the display name of the version that follows the specified version.
        /// Used for "removed in X" messages.
        /// </summary>
        internal static string GetNextVersionDisplayName(LuaCompatibilityVersion version)
        {
            return version switch
            {
                LuaCompatibilityVersion.Lua51 => "Lua 5.2",
                LuaCompatibilityVersion.Lua52 => "Lua 5.3",
                LuaCompatibilityVersion.Lua53 => "Lua 5.4",
                LuaCompatibilityVersion.Lua54 => "Lua 5.5",
                LuaCompatibilityVersion.Lua55 => "Lua 5.6",
                LuaCompatibilityVersion.Latest => "Lua 5.5",
                _ => throw new InvalidEnumArgumentException(
                    nameof(version),
                    (int)version,
                    typeof(LuaCompatibilityVersion)
                ),
            };
        }
    }
}
