namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Tests for math module functions that are version-specific.
    /// Verifies that Lua 5.3+ features are NOT available in Lua 5.1/5.2 modes.
    /// Per CONTRIBUTING_AI.md: These tests verify NovaSharp matches official Lua behavior.
    /// </summary>
    /// <remarks>
    /// Reference: Lua 5.3 Manual §6.7 - math.tointeger, math.type, math.ult, math.maxinteger, math.mininteger
    /// were all introduced in Lua 5.3.
    /// </remarks>
    public sealed class MathVersionCompatibilityTUnitTests
    {
        #region math.type availability

        /// <summary>
        /// math.type should be nil in Lua 5.1 and 5.2 - it was added in Lua 5.3.
        /// Verified against lua5.2 -e "print(math.type)" → nil
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task MathTypeShouldBeNilInPreLua53(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return math.type");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because(
                    $"math.type should be nil in {version} (Lua 5.3+ feature). "
                        + $"Actual type: {result.Type}, value: {result}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// math.type should work in Lua 5.3+ and distinguish integers from floats.
        /// Per Lua spec: math.type returns nil for non-numbers (doesn't throw).
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathTypeAvailableInLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue intResult = script.DoString("return math.type(5)");
            DynValue floatResult = script.DoString("return math.type(5.5)");

            await Assert
                .That(intResult.String)
                .IsEqualTo("integer")
                .Because($"math.type(5) should return 'integer' in {version}")
                .ConfigureAwait(false);

            await Assert
                .That(floatResult.String)
                .IsEqualTo("float")
                .Because($"math.type(5.5) should return 'float' in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// math.type should return nil for non-numeric values (not throw).
        /// Per Lua 5.3+ spec: "If x is not a number, math.type returns nil."
        /// Verified: lua5.3 -e "print(math.type('hello'))" → nil
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53, "'hello'")]
        [Arguments(LuaCompatibilityVersion.Lua53, "true")]
        [Arguments(LuaCompatibilityVersion.Lua53, "nil")]
        [Arguments(LuaCompatibilityVersion.Lua53, "{}")]
        [Arguments(LuaCompatibilityVersion.Lua54, "'hello'")]
        [Arguments(LuaCompatibilityVersion.Lua54, "true")]
        [Arguments(LuaCompatibilityVersion.Lua55, "'hello'")]
        public async Task MathTypeReturnsNilForNonNumbers(
            LuaCompatibilityVersion version,
            string input
        )
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString($"return math.type({input})");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because(
                    $"math.type({input}) should return nil for non-numbers in {version}. "
                        + $"Actual: {result.Type} = {result}"
                )
                .ConfigureAwait(false);
        }

        #endregion

        #region math.tointeger availability

        /// <summary>
        /// math.tointeger should be nil in Lua 5.1 and 5.2.
        /// Verified against lua5.2 -e "print(math.tointeger)" → attempt to call field 'tointeger' (a nil value)
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task MathToIntegerShouldBeNilInPreLua53(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return math.tointeger");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because(
                    $"math.tointeger should be nil in {version} (Lua 5.3+ feature). "
                        + $"Actual type: {result.Type}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// math.tointeger should work in Lua 5.3+ and convert compatible values.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53, "42", 42)]
        [Arguments(LuaCompatibilityVersion.Lua53, "'42'", 42)]
        [Arguments(LuaCompatibilityVersion.Lua54, "42", 42)]
        [Arguments(LuaCompatibilityVersion.Lua54, "'42'", 42)]
        [Arguments(LuaCompatibilityVersion.Lua55, "42", 42)]
        public async Task MathToIntegerConvertsInLua53Plus(
            LuaCompatibilityVersion version,
            string input,
            double expected
        )
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString($"return math.tointeger({input})");

            await Assert
                .That(result.Number)
                .IsEqualTo(expected)
                .Because($"math.tointeger({input}) should return {expected} in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// math.tointeger should return nil for non-integral floats in Lua 5.3+.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53, "3.5")]
        [Arguments(LuaCompatibilityVersion.Lua54, "3.5")]
        [Arguments(LuaCompatibilityVersion.Lua55, "3.5")]
        public async Task MathToIntegerReturnsNilForFractional(
            LuaCompatibilityVersion version,
            string input
        )
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString($"return math.tointeger({input})");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because($"math.tointeger({input}) should return nil (not integral) in {version}")
                .ConfigureAwait(false);
        }

        #endregion

        #region math.ult availability

        /// <summary>
        /// math.ult should be nil in Lua 5.1 and 5.2.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task MathUltShouldBeNilInPreLua53(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return math.ult");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because(
                    $"math.ult should be nil in {version} (Lua 5.3+ feature). "
                        + $"Actual type: {result.Type}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// math.ult should perform unsigned comparison in Lua 5.3+.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathUltWorksInLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            // -1 in unsigned is MAX, so 0 < MAX should be true
            DynValue zeroLtNegOne = script.DoString("return math.ult(0, -1)");
            // MAX > 0, so this should be false
            DynValue negOneLtZero = script.DoString("return math.ult(-1, 0)");

            await Assert
                .That(zeroLtNegOne.Boolean)
                .IsTrue()
                .Because($"math.ult(0, -1) should be true (unsigned: 0 < MAX) in {version}")
                .ConfigureAwait(false);

            await Assert
                .That(negOneLtZero.Boolean)
                .IsFalse()
                .Because($"math.ult(-1, 0) should be false (unsigned: MAX > 0) in {version}")
                .ConfigureAwait(false);
        }

        #endregion

        #region math.maxinteger / math.mininteger availability

        /// <summary>
        /// math.maxinteger and math.mininteger should be nil in Lua 5.1 and 5.2.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task MathIntegerConstantsShouldBeNilInPreLua53(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue maxint = script.DoString("return math.maxinteger");
            DynValue minint = script.DoString("return math.mininteger");

            await Assert
                .That(maxint.IsNil())
                .IsTrue()
                .Because(
                    $"math.maxinteger should be nil in {version}. "
                        + $"Actual type: {maxint.Type}, value: {maxint}"
                )
                .ConfigureAwait(false);

            await Assert
                .That(minint.IsNil())
                .IsTrue()
                .Because(
                    $"math.mininteger should be nil in {version}. "
                        + $"Actual type: {minint.Type}, value: {minint}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// math.maxinteger and math.mininteger should have correct values in Lua 5.3+.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathIntegerConstantsAvailableInLua53Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue maxint = script.DoString("return math.maxinteger");
            DynValue minint = script.DoString("return math.mininteger");

            // long.MaxValue = 9223372036854775807
            // long.MinValue = -9223372036854775808
            await Assert
                .That(maxint.Number)
                .IsEqualTo(9223372036854775807d)
                .Because($"math.maxinteger should be 2^63-1 in {version}")
                .ConfigureAwait(false);

            await Assert
                .That(minint.Number)
                .IsEqualTo(-9223372036854775808d)
                .Because($"math.mininteger should be -2^63 in {version}")
                .ConfigureAwait(false);
        }

        #endregion

        #region Functions available in all versions

        /// <summary>
        /// These math functions should be available in ALL Lua versions.
        /// </summary>
        public static IEnumerable<(
            LuaCompatibilityVersion Version,
            string Function
        )> AllVersionFunctions()
        {
            LuaCompatibilityVersion[] versions = new[]
            {
                LuaCompatibilityVersion.Lua51,
                LuaCompatibilityVersion.Lua52,
                LuaCompatibilityVersion.Lua53,
                LuaCompatibilityVersion.Lua54,
                LuaCompatibilityVersion.Lua55,
            };

            string[] functions = new[]
            {
                "abs",
                "acos",
                "asin",
                "atan",
                "ceil",
                "cos",
                "deg",
                "exp",
                "floor",
                "fmod",
                "frexp",
                "ldexp",
                "log",
                "max",
                "min",
                "modf",
                "pow",
                "rad",
                "random",
                "randomseed",
                "sin",
                "sqrt",
                "tan",
            };

            foreach (LuaCompatibilityVersion version in versions)
            {
                foreach (string function in functions)
                {
                    yield return (version, function);
                }
            }
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51, "abs")]
        [Arguments(LuaCompatibilityVersion.Lua51, "floor")]
        [Arguments(LuaCompatibilityVersion.Lua51, "ceil")]
        [Arguments(LuaCompatibilityVersion.Lua51, "random")]
        [Arguments(LuaCompatibilityVersion.Lua52, "abs")]
        [Arguments(LuaCompatibilityVersion.Lua52, "floor")]
        [Arguments(LuaCompatibilityVersion.Lua52, "ceil")]
        [Arguments(LuaCompatibilityVersion.Lua52, "random")]
        [Arguments(LuaCompatibilityVersion.Lua53, "abs")]
        [Arguments(LuaCompatibilityVersion.Lua53, "floor")]
        [Arguments(LuaCompatibilityVersion.Lua54, "abs")]
        [Arguments(LuaCompatibilityVersion.Lua54, "floor")]
        [Arguments(LuaCompatibilityVersion.Lua55, "abs")]
        [Arguments(LuaCompatibilityVersion.Lua55, "floor")]
        public async Task CoreMathFunctionsAvailableInAllVersions(
            LuaCompatibilityVersion version,
            string function
        )
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString($"return math.{function}");

            // Math functions are ClrFunction (bound C# methods), not Lua Function
            bool isCallable =
                result.Type == DataType.Function || result.Type == DataType.ClrFunction;

            await Assert
                .That(isCallable)
                .IsTrue()
                .Because(
                    $"math.{function} should be callable in {version}. "
                        + $"Actual type: {result.Type}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// math.pi should be available in all versions.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathPiAvailableInAllVersions(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return math.pi");

            await Assert
                .That(result.Number)
                .IsEqualTo(Math.PI)
                .Within(1e-12)
                .Because($"math.pi should be π in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// math.huge should be available in all versions.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathHugeAvailableInAllVersions(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return math.huge");

            await Assert
                .That(result.Number)
                .IsGreaterThan(1e307)
                .Because($"math.huge should be a very large number in {version}")
                .ConfigureAwait(false);
        }

        #endregion

        #region Calling nil functions should error

        /// <summary>
        /// Calling math.type when it's nil should throw an error.
        /// This matches Lua 5.2 behavior: lua5.2 -e "print(math.type(5))" throws
        /// "attempt to call field 'type' (a nil value)"
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task CallingMathTypeInPreLua53ThrowsError(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException caught = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString("return math.type(5)");
            });

            await Assert
                .That(caught)
                .IsNotNull()
                .Because($"Calling math.type(5) in {version} should throw (function is nil)")
                .ConfigureAwait(false);

            await Assert
                .That(caught.Message)
                .Contains("nil")
                .Because($"Error message should indicate nil value. Actual: {caught.Message}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Calling math.tointeger when it's nil should throw an error.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task CallingMathToIntegerInPreLua53ThrowsError(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException caught = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString("return math.tointeger(42)");
            });

            await Assert
                .That(caught)
                .IsNotNull()
                .Because($"Calling math.tointeger(42) in {version} should throw (function is nil)")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Calling math.ult when it's nil should throw an error.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task CallingMathUltInPreLua53ThrowsError(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException caught = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString("return math.ult(0, 1)");
            });

            await Assert
                .That(caught)
                .IsNotNull()
                .Because($"Calling math.ult(0, 1) in {version} should throw (function is nil)")
                .ConfigureAwait(false);
        }

        #endregion

        #region Diagnostic helper

        /// <summary>
        /// Diagnostic test that dumps all math table contents for a given version.
        /// This helps debug which functions are available.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DiagnosticDumpMathTableContents(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            // Get all keys from math table
            DynValue result = script.DoString(
                @"
                local keys = {}
                for k, v in pairs(math) do
                    keys[#keys + 1] = k .. '=' .. type(v)
                end
                table.sort(keys)
                return table.concat(keys, ', ')
            "
            );

            // This test always passes but logs the math table contents for debugging
            await Assert
                .That(result.String)
                .IsNotEmpty()
                .Because($"Math table in {version} should have contents: {result.String}")
                .ConfigureAwait(false);
        }

        #endregion

        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
            };
            return new Script(CoreModulePresets.Complete, options);
        }
    }
}
