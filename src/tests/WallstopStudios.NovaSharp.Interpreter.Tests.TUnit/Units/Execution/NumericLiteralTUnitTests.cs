namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Numeric literal materialization, float formatting, base conversions, and
    /// <c>table.concat</c> number rendering verified against reference Lua 5.1-5.5.
    /// </summary>
    public sealed class NumericLiteralTUnitTests
    {
        private static readonly LuaCompatibilityVersion[] AllVersions =
        {
            LuaCompatibilityVersion.Lua51,
            LuaCompatibilityVersion.Lua52,
            LuaCompatibilityVersion.Lua53,
            LuaCompatibilityVersion.Lua54,
            LuaCompatibilityVersion.Lua55,
        };

        [global::TUnit.Core.Test]
        [MethodDataSource(nameof(GetLiteralData))]
        public async Task NumericLiteralsMaterializePerCompatibilityVersion(
            LuaCompatibilityVersion version,
            string literal,
            bool expectInteger,
            string expectPrinted
        )
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                "return {literal}".Replace("{literal}", literal, StringComparison.Ordinal)
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsEqualTo(expectInteger).ConfigureAwait(false);
            await Assert
                .That(result.ToPrintString(version))
                .IsEqualTo(expectPrinted)
                .ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(LuaCompatibilityVersion, string, bool, string)> GetLiteralData()
        {
            // Lua 5.1/5.2 have a single double number type: integer-syntax literals round
            // to IEEE 754; 5.3+ keep integer subtypes, with hex wrapping modulo 2^64.
            // Decimal literals beyond lua_Integer fall back to floats in 5.3+.
            (string Literal, string Pre53, string Lua53Plus, bool Lua53PlusInteger)[] cases =
            {
                ("0", "0", "0", true),
                ("9007199254740993", "9.007199254741e+15", "9007199254740993", true),
                ("9223372036854775807", "9.2233720368548e+18", "9223372036854775807", true),
                ("18446744073709551616", "1.844674407371e+19", "1.844674407371e+19", false),
                ("99999999999999999999", "1e+20", "1e+20", false),
                ("0x89abcdef", "2309737967", "2309737967", true),
                ("0xffffffffffffffff", "1.844674407371e+19", "-1", true),
                ("0x10000000000000000", "1.844674407371e+19", "0", true),
                ("0xdeadbeefdeadbeefdeadbeef", "6.8915718021581e+28", "-2401053088876216593", true),
                ("123456789012345678", "1.2345678901235e+17", "123456789012345678", true),
            };

            foreach (LuaCompatibilityVersion version in AllVersions)
            {
                foreach (
                    (string literal, string pre53, string lua53Plus, bool lua53PlusInteger) in cases
                )
                {
                    bool expectInteger =
                        version >= LuaCompatibilityVersion.Lua53 && lua53PlusInteger;
                    string expectPrinted =
                        version >= LuaCompatibilityVersion.Lua53 ? lua53Plus : pre53;
                    // Lua 5.5 formats unroundtrippable floats with 17 digits
                    if (
                        version == LuaCompatibilityVersion.Lua55
                        && literal == "18446744073709551616"
                    )
                    {
                        expectPrinted = "1.8446744073709552e+19";
                    }

                    yield return (version, literal, expectInteger, expectPrinted);
                }
            }
        }

        [global::TUnit.Core.Test]
        [MethodDataSource(nameof(GetFloatFormatData))]
        public async Task FloatsFormatLikeReferenceTostring(
            LuaCompatibilityVersion version,
            string luaExpression,
            string expected
        )
        {
            await Assert
                .That(EvaluateExpression(version, luaExpression))
                .IsEqualTo(expected)
                .ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(LuaCompatibilityVersion, string, string)> GetFloatFormatData()
        {
            // Lua 5.1-5.4 print floats with %.14g and Lua 5.5 from %.15g (falling back
            // to %.17g when the shorter form does not round-trip); Lua 5.3+ additionally
            // append ".0" when the formatted result looks like an integer.
            (string Expression, string V51To52, string V53To54, string V55)[] cases =
            {
                ("1/3", "0.33333333333333", "0.33333333333333", "0.33333333333333331"),
                ("1e15", "1e+15", "1e+15", "1e+15"),
                ("2^53", "9.007199254741e+15", "9.007199254741e+15", "9007199254740992.0"),
                ("2.0", "2", "2.0", "2.0"),
                ("-0.0", "-0", "-0.0", "-0.0"),
                ("0.1", "0.1", "0.1", "0.1"),
                ("1e14", "1e+14", "1e+14", "100000000000000.0"),
                (
                    "123456789012345.0",
                    "1.2345678901234e+14",
                    "1.2345678901234e+14",
                    "123456789012345.0"
                ),
                ("1234567.891234567", "1234567.8912346", "1234567.8912346", "1234567.8912345669"),
                ("1.0000000000000002", "1", "1.0", "1.0000000000000002"),
                (
                    "4.9e-324",
                    "4.9406564584125e-324",
                    "4.9406564584125e-324",
                    "4.94065645841247e-324"
                ),
            };

            foreach (LuaCompatibilityVersion version in AllVersions)
            {
                foreach ((string expression, string v51To52, string v53To54, string v55) in cases)
                {
                    string expected = version switch
                    {
                        var v when v >= LuaCompatibilityVersion.Lua55 => v55,
                        var v when v >= LuaCompatibilityVersion.Lua53 => v53To54,
                        _ => v51To52,
                    };
                    yield return (version, expression, expected);
                }
            }
        }

        [global::TUnit.Core.Test]
        [MethodDataSource(nameof(GetToNumberBaseData))]
        public async Task ToNumberWithBaseMatchesReference(
            LuaCompatibilityVersion version,
            string valueExpression,
            string expected
        )
        {
            await Assert
                .That(EvaluateExpression(version, valueExpression))
                .IsEqualTo(expected)
                .ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(LuaCompatibilityVersion, string, string)> GetToNumberBaseData()
        {
            // Lua 5.1 defers to strtoul (0x prefix in base 16, unsigned wraparound,
            // saturation, base 10 = standard conversion); Lua 5.2 accumulates in double
            // with signed negation; Lua 5.3+ wrap modulo 2^64 and keep the integer
            // subtype. Reference 5.1 saturates at the platform's unsigned long width:
            // 32 bits in reference Windows builds, 64 bits on LP64 platforms.
            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows
            );
            string v51Saturation = isWindows ? "4294967295" : "1.844674407371e+19";
            string v51NegativeWrap = isWindows ? "4294967041" : "1.844674407371e+19";
            string v51LargeNumberCoercion = isWindows ? "4294967295" : "285960729237";
            (string Expression, string V51, string V52, string V53Plus)[] cases =
            {
                ("tonumber('7f', 16)", "127", "127", "127"),
                ("tonumber('-ff', 16)", v51NegativeWrap, "-255", "-255"),
                ("tonumber('ffffffffffffffff', 16)", v51Saturation, "1.844674407371e+19", "-1"),
                ("tonumber('10000000000000000', 16)", v51Saturation, "1.844674407371e+19", "0"),
                ("tonumber('fffffffffffffffff', 16)", v51Saturation, "2.9514790517935e+20", "-1"),
                ("tonumber('0x11', 10)", "17", "nil", "nil"),
                ("tonumber('3.14', 10)", "3.14", "nil", "nil"),
                ("tonumber('0x10', 16)", "16", "nil", "nil"),
                ("tonumber('7g', 16)", "nil", "nil", "nil"),
                ("tonumber('17', 6)", "nil", "nil", "nil"),
                ("tonumber(111, 2)", "7", "7", "nil"),
                (
                    "tonumber(4294967295, 16)",
                    v51LargeNumberCoercion,
                    "285960729237",
                    "285960729237"
                ),
                ("tonumber('0x10')", "16", "16", "16"),
            };

            foreach (LuaCompatibilityVersion version in AllVersions)
            {
                foreach ((string expression, string v51, string v52, string v53Plus) in cases)
                {
                    // Lua 5.3+ reject number arguments outright (checked separately below)
                    if (
                        version >= LuaCompatibilityVersion.Lua53
                        && (
                            expression == "tonumber(111, 2)"
                            || expression == "tonumber(4294967295, 16)"
                        )
                    )
                    {
                        continue;
                    }

                    string expected = version switch
                    {
                        var v when v == LuaCompatibilityVersion.Lua51 => v51,
                        var v when v == LuaCompatibilityVersion.Lua52 => v52,
                        _ => v53Plus,
                    };
                    yield return (version, expression, expected);
                }
            }
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ToNumberWithBaseRejectsNumberArgumentsInLua53Plus(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    "return tonumber({expression})".Replace(
                        "{expression}",
                        "111, 2",
                        StringComparison.Ordinal
                    )
                )
            )!;

            await Assert
                .That(exception.Message)
                .Contains("string expected, got number")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task TableConcatFormatsNumbersLikeTostring(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            string expected = version switch
            {
                var v when v <= LuaCompatibilityVersion.Lua52 =>
                    "2,-5,3.5,1e+100,0.1,0.33333333333333,1e+14,9.007199254741e+15",
                var v when v <= LuaCompatibilityVersion.Lua54 =>
                    "2.0,-5.0,3.5,1e+100,0.1,0.33333333333333,1e+14,9.007199254741e+15",
                _ =>
                    "2.0,-5.0,3.5,1e+100,0.1,0.33333333333333331,100000000000000.0,9007199254740992.0",
            };

            LuaValue result = script.DoString(
                "return table.concat({2.0, -5.0, 3.5, 1e100, 0.1, 1/3, 1e14, 2^53}, \",\")"
            );

            await Assert.That(result.String).IsEqualTo(expected).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [MethodDataSource(nameof(GetCoercionData))]
        public async Task NumberToStringCoercionMatchesReference(
            LuaCompatibilityVersion version,
            string luaExpression,
            string expected
        )
        {
            await Assert
                .That(EvaluateExpression(version, luaExpression))
                .IsEqualTo(expected)
                .ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "TUnit MethodDataSource requires method"
        )]
        public static IEnumerable<(LuaCompatibilityVersion, string, string)> GetCoercionData()
        {
            // luaL_checklstring coercion formats numbers like tostring: integer-syntax
            // literals print bare in 5.1/5.2 (float subtype, %.14g) and via the integer
            // subtype in 5.3+; float-syntax values keep the ".0" distinction in 5.3+.
            (string Expression, string V51To52, string V53Plus)[] cases =
            {
                ("string.len(42)", "2", "2"),
                ("string.len(2.0)", "1", "3"),
                ("string.rep(2.0, 2)", "22", "2.02.0"),
                ("string.find('a2b', 2.0) and 'found' or 'nil'", "found", "nil"),
                ("table.concat({1}, 2.0) .. ''", "1", "1"),
                ("table.concat({1, 2}, 2.0)", "122", "12.02"),
            };

            foreach (LuaCompatibilityVersion version in AllVersions)
            {
                foreach ((string expression, string v51To52, string v53Plus) in cases)
                {
                    yield return (
                        version,
                        expression,
                        version >= LuaCompatibilityVersion.Lua53 ? v53Plus : v51To52
                    );
                }
            }
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task ToNumberTruncatesFractionalBaseBeforeLua53(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);

            LuaValue result = script.DoString(
                "return tonumber('12', {base})".Replace("{base}", "3.5", StringComparison.Ordinal)
            );

            await Assert.That(result.ToPrintString(version)).IsEqualTo("5").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ToNumberRejectsFractionalBaseFromLua53(LuaCompatibilityVersion version)
        {
            Script script = new(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    "return tonumber('12', {base})".Replace(
                        "{base}",
                        "3.5",
                        StringComparison.Ordinal
                    )
                )
            )!;

            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Evaluates a Lua expression and returns its <c>tostring</c> rendering. The
        /// unresolved <c>{expression}</c> placeholder keeps the corpus extractor from
        /// emitting a partial comparable snippet; reference-verified coverage lives in the
        /// <c>LuaFixtures/NumericLiteralTUnitTests</c> fixtures.
        /// </summary>
        private static string EvaluateExpression(LuaCompatibilityVersion version, string expression)
        {
            Script script = new(version);
            return script
                .DoString(
                    "return tostring({expression})".Replace(
                        "{expression}",
                        expression,
                        StringComparison.Ordinal
                    )
                )
                .String;
        }
    }
}
