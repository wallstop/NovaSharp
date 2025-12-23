namespace WallstopStudios.NovaSharp.Interpreter.Utilities
{
    using System;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Provides helper methods for Lua-compatible numeric operations with version-aware behavior.
    /// </summary>
    internal static class LuaNumberHelpers
    {
        /// <summary>
        /// Validates that a <see cref="LuaNumber"/> has an exact integer representation for Lua 5.3+ semantics.
        /// </summary>
        /// <param name="value">The LuaNumber value to validate.</param>
        /// <param name="functionName">The function name for error messages (e.g., "byte").</param>
        /// <param name="argIndex">1-based argument index for error messages.</param>
        /// <exception cref="ScriptRuntimeException">
        /// Thrown when the value is NaN, Infinity, a fractional float, or a float that cannot
        /// be exactly converted to an integer (values outside ±2^53 range or overflowing long).
        /// </exception>
        /// <remarks>
        /// This method implements the Lua 5.3+ requirement that certain arguments must have
        /// an exact integer representation. The behavior differs based on the LuaNumber subtype:
        /// <list type="bullet">
        ///   <item>Integer subtype: Always valid (already has integer representation).</item>
        ///   <item>Float subtype: Valid only if it represents a whole number that can be exactly
        ///   converted to a 64-bit integer. This excludes NaN, Infinity, fractional values,
        ///   and floats outside the range where doubles can exactly represent all integers (±2^53)
        ///   unless the float value is exactly equal to a valid integer.</item>
        /// </list>
        /// </remarks>
        public static void RequireIntegerRepresentation(
            LuaNumber value,
            string functionName,
            int argIndex
        )
        {
            // Integer subtype always has integer representation
            if (value.IsInteger)
            {
                return;
            }

            // Float subtype: need to check if it can be exactly converted
            double floatValue = value.AsFloat;

            // NaN and infinity cannot be integers
            if (double.IsNaN(floatValue) || double.IsInfinity(floatValue))
            {
                throw new ScriptRuntimeException(
                    ZString.Concat(
                        "bad argument #",
                        argIndex,
                        " to '",
                        functionName,
                        "' (number has no integer representation)"
                    )
                );
            }

            // Check if it's a whole number
            double floored = Math.Floor(floatValue);
            if (floored != floatValue)
            {
                throw new ScriptRuntimeException(
                    ZString.Concat(
                        "bad argument #",
                        argIndex,
                        " to '",
                        functionName,
                        "' (number has no integer representation)"
                    )
                );
            }

            // Check if it's within the valid integer range.
            // IMPORTANT: We must use explicit constants for the range check because:
            // 1. (double)long.MaxValue == 9223372036854775808.0 (which is long.MaxValue + 1 due to rounding)
            // 2. This makes (double)long.MaxValue equal to 2^63, which CANNOT fit in a long
            // 3. Casting a double >= 2^63 to long produces undefined behavior (platform-dependent)
            //    - On x64: wraps to long.MinValue
            //    - On ARM64: may saturate to long.MaxValue (passing round-trip falsely)
            //
            // The valid range for conversion is: -2^63 <= value < 2^63
            // Note: -2^63 is exactly representable as long.MinValue
            // Note: 2^63 - 1 (long.MaxValue) rounds up to 2^63 when stored as double
            const double TwoPow63 = 9223372036854775808.0; // 2^63, first value that doesn't fit in signed long
            if (floatValue < -TwoPow63 || floatValue >= TwoPow63)
            {
                // Outside valid long range
                throw new ScriptRuntimeException(
                    ZString.Concat(
                        "bad argument #",
                        argIndex,
                        " to '",
                        functionName,
                        "' (number has no integer representation)"
                    )
                );
            }

            // At this point, floatValue is in [-2^63, 2^63), so (long) cast is well-defined.
            // Verify round-trip preserves value to catch precision loss.
            // This handles cases where the float is within range but isn't exactly representable
            // as an integer (e.g., values > 2^53 that lost precision during earlier operations).
            long asLong = (long)floatValue;
            if ((double)asLong != floatValue)
            {
                throw new ScriptRuntimeException(
                    ZString.Concat(
                        "bad argument #",
                        argIndex,
                        " to '",
                        functionName,
                        "' (number has no integer representation)"
                    )
                );
            }
        }

        /// <summary>
        /// Validates that a <see cref="DynValue"/> has an exact integer representation for Lua 5.3+ semantics.
        /// </summary>
        /// <param name="dynValue">The DynValue to validate (must be a number type).</param>
        /// <param name="functionName">The function name for error messages.</param>
        /// <param name="argIndex">1-based argument index for error messages.</param>
        /// <exception cref="ScriptRuntimeException">
        /// Thrown when the value is NaN, Infinity, a fractional float, or a float that cannot
        /// be exactly converted to an integer.
        /// </exception>
        /// <remarks>
        /// This overload uses the <see cref="LuaNumber"/> from the DynValue to properly
        /// distinguish between integer and float subtypes. Nil values are allowed and
        /// handled by the caller's default argument logic.
        /// </remarks>
        public static void RequireIntegerRepresentation(
            DynValue dynValue,
            string functionName,
            int argIndex
        )
        {
            if (dynValue == null || dynValue.IsNil())
            {
                return; // Nil values are handled by default argument logic
            }

            RequireIntegerRepresentation(dynValue.LuaNumber, functionName, argIndex);
        }

        /// <summary>
        /// Validates string index arguments according to the script's Lua compatibility version.
        /// </summary>
        /// <param name="version">The Lua compatibility version of the script.</param>
        /// <param name="startValue">The start index DynValue (may be nil).</param>
        /// <param name="endValue">The end index DynValue (may be nil).</param>
        /// <param name="functionName">The function name for error messages.</param>
        /// <remarks>
        /// In Lua 5.3+, non-integer index arguments cause an error with the message
        /// "number has no integer representation". In Lua 5.1/5.2, non-integer indices
        /// are silently truncated via floor.
        /// </remarks>
        public static void ValidateStringIndices(
            LuaCompatibilityVersion version,
            DynValue startValue,
            DynValue endValue,
            string functionName
        )
        {
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);
            if (resolved < LuaCompatibilityVersion.Lua53)
            {
                return; // Lua 5.1/5.2: silently truncate, no validation needed
            }

            // Lua 5.3+: require exact integer representation
            if (startValue != null && !startValue.IsNil())
            {
                RequireIntegerRepresentation(startValue.LuaNumber, functionName, 2);
            }

            if (endValue != null && !endValue.IsNil())
            {
                RequireIntegerRepresentation(endValue.LuaNumber, functionName, 3);
            }
        }

        /// <summary>
        /// Validates a single numeric argument according to the script's Lua compatibility version.
        /// </summary>
        /// <param name="version">The Lua compatibility version of the script.</param>
        /// <param name="value">The DynValue to validate (may be nil).</param>
        /// <param name="functionName">The function name for error messages.</param>
        /// <param name="argIndex">1-based argument index for error messages.</param>
        /// <remarks>
        /// In Lua 5.3+, non-integer numeric arguments to certain functions cause an error with
        /// the message "number has no integer representation". In Lua 5.1/5.2, non-integer values
        /// are silently truncated via floor.
        /// </remarks>
        public static void ValidateIntegerArgument(
            LuaCompatibilityVersion version,
            DynValue value,
            string functionName,
            int argIndex
        )
        {
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);
            if (resolved < LuaCompatibilityVersion.Lua53)
            {
                return; // Lua 5.1/5.2: silently truncate, no validation needed
            }

            // Lua 5.3+: require exact integer representation
            if (value != null && !value.IsNil())
            {
                RequireIntegerRepresentation(value.LuaNumber, functionName, argIndex);
            }
        }

        /// <summary>
        /// Extracts a long integer from a DynValue with version-aware validation.
        /// </summary>
        /// <param name="version">The Lua compatibility version of the script.</param>
        /// <param name="value">The DynValue to extract (must be a number).</param>
        /// <param name="functionName">The function name for error messages.</param>
        /// <param name="argIndex">1-based argument index for error messages.</param>
        /// <returns>The extracted long integer value.</returns>
        /// <remarks>
        /// <para>
        /// <b>Lua 5.3+</b>: Throws "number has no integer representation" if the value
        /// is NaN, Infinity, or a non-integral float. Integral floats are allowed.
        /// </para>
        /// <para>
        /// <b>Lua 5.1/5.2</b>: Silently truncates via floor. NaN becomes 0 (due to (long) cast behavior),
        /// Infinity causes overflow behavior.
        /// </para>
        /// </remarks>
        public static long ToLongWithValidation(
            LuaCompatibilityVersion version,
            DynValue value,
            string functionName,
            int argIndex
        )
        {
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);
            LuaNumber luaNum = value.LuaNumber;

            if (resolved >= LuaCompatibilityVersion.Lua53)
            {
                // Lua 5.3+: require exact integer representation
                RequireIntegerRepresentation(luaNum, functionName, argIndex);
            }

            // Extract the value - for integers this preserves precision
            if (luaNum.IsInteger)
            {
                return luaNum.AsInteger;
            }

            // For floats, floor and convert (Lua 5.1/5.2 behavior, or validated integral float in 5.3+)
            return (long)Math.Floor(luaNum.AsFloat);
        }
    }
}
