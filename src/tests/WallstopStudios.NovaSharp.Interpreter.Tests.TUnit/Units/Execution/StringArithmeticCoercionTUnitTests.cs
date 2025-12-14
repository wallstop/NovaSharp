namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Tests for string-to-number coercion in arithmetic operations.
    /// Per Lua 5.4 spec: String-to-number coercion was removed from arithmetic operators.
    /// Instead, the string metatable provides arithmetic metamethods (__add, __sub, etc.)
    /// that perform the coercion.
    /// Per CONTRIBUTING_AI.md: These tests verify NovaSharp matches official Lua behavior.
    /// </summary>
    /// <remarks>
    /// Reference: Lua 5.4 Manual §3.4.1 - Coercion and conversion
    /// In Lua 5.1-5.3, arithmetic operations automatically coerce string operands to numbers.
    /// In Lua 5.4+, this coercion is provided by string metatable metamethods.
    /// The end result is the same ("10" + 1 == 11), but the mechanism differs.
    /// </remarks>
    public sealed class StringArithmeticCoercionTUnitTests
    {
        #region String metatable metamethods availability

        /// <summary>
        /// In Lua 5.1-5.3, the string metatable should NOT have arithmetic metamethods.
        /// String-to-number coercion is built into the operators themselves.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task StringMetatableHasNoArithmeticMetamethodsInPreLua54(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString(
                @"
                local mt = getmetatable('')
                return mt and mt.__add or nil
            "
            );

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because(
                    $"String metatable should not have __add in {version}. "
                        + "Coercion is built into the operators."
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// In Lua 5.4+, the string metatable should have arithmetic metamethods.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StringMetatableHasArithmeticMetamethodsInLua54Plus(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString(
                @"
                local mt = getmetatable('')
                local hasAll = mt and
                    mt.__add and
                    mt.__sub and
                    mt.__mul and
                    mt.__div and
                    mt.__mod and
                    mt.__pow and
                    mt.__idiv and
                    mt.__unm
                return hasAll ~= nil
            "
            );

            await Assert
                .That(result.Boolean)
                .IsTrue()
                .Because(
                    $"String metatable should have all arithmetic metamethods in {version}. "
                        + "These provide string-to-number coercion."
                )
                .ConfigureAwait(false);
        }

        #endregion

        #region Basic arithmetic works with strings (all versions)

        /// <summary>
        /// String-to-number coercion should work in all Lua versions.
        /// The mechanism differs (built-in vs metamethod), but the result is the same.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StringAdditionWorksInAllVersions(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return '10' + 1");

            await Assert
                .That(result.Number)
                .IsEqualTo(11.0)
                .Because($"'10' + 1 should equal 11 in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Test all arithmetic operations with string operands.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task AllArithmeticOperationsWorkWithStrings(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            // Test addition
            DynValue addResult = script.DoString("return '10' + 1");
            await Assert
                .That(addResult.Number)
                .IsEqualTo(11.0)
                .Because("String addition should work")
                .ConfigureAwait(false);

            // Test subtraction
            DynValue subResult = script.DoString("return '10' - 1");
            await Assert
                .That(subResult.Number)
                .IsEqualTo(9.0)
                .Because("String subtraction should work")
                .ConfigureAwait(false);

            // Test multiplication
            DynValue mulResult = script.DoString("return '10' * 2");
            await Assert
                .That(mulResult.Number)
                .IsEqualTo(20.0)
                .Because("String multiplication should work")
                .ConfigureAwait(false);

            // Test division
            DynValue divResult = script.DoString("return '10' / 2");
            await Assert
                .That(divResult.Number)
                .IsEqualTo(5.0)
                .Because("String division should work")
                .ConfigureAwait(false);

            // Test modulo
            DynValue modResult = script.DoString("return '10' % 3");
            await Assert
                .That(modResult.Number)
                .IsEqualTo(1.0)
                .Because("String modulo should work")
                .ConfigureAwait(false);

            // Test power
            DynValue powResult = script.DoString("return '2' ^ 3");
            await Assert
                .That(powResult.Number)
                .IsEqualTo(8.0)
                .Because("String power should work")
                .ConfigureAwait(false);

            // Test unary minus
            DynValue unmResult = script.DoString("return -'10'");
            await Assert
                .That(unmResult.Number)
                .IsEqualTo(-10.0)
                .Because("String unary minus should work")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Floor division should work with strings in Lua 5.3+.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FloorDivisionWorksWithStrings(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString("return '10' // 3");

            await Assert
                .That(result.Number)
                .IsEqualTo(3.0)
                .Because($"'10' // 3 should equal 3 in {version}")
                .ConfigureAwait(false);
        }

        #endregion

        #region Custom metamethod override (Lua 5.4+ specific)

        /// <summary>
        /// In Lua 5.4+, custom string arithmetic metamethods should be called.
        /// This verifies that metamethods are used, not built-in coercion.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CustomStringMetamethodIsCalledInLua54Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString(
                @"
                local mt = getmetatable('')
                local original_add = mt.__add
                local called = false
                mt.__add = function(a, b)
                    called = true
                    return original_add(a, b)
                end
                local sum = '10' + 1
                mt.__add = original_add  -- restore original
                return called, sum
            "
            );

            await Assert
                .That(result.Tuple[0].Boolean)
                .IsTrue()
                .Because($"Custom __add metamethod should be called in {version}")
                .ConfigureAwait(false);

            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(11.0)
                .Because("Result should still be 11")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// In Lua 5.1-5.3, custom string arithmetic metamethods on the string metatable
        /// are NOT called because coercion is built into the operators.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task CustomStringMetamethodNotCalledInPreLua54(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString(
                @"
                local mt = getmetatable('')
                local called = false
                mt.__add = function(a, b)
                    called = true
                    return tonumber(a) + tonumber(b)
                end
                local sum = '10' + 1
                mt.__add = nil  -- cleanup
                return called, sum
            "
            );

            await Assert
                .That(result.Tuple[0].Boolean)
                .IsFalse()
                .Because(
                    $"Custom __add metamethod should NOT be called in {version}. "
                        + "Built-in coercion handles it."
                )
                .ConfigureAwait(false);

            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(11.0)
                .Because("Result should still be 11")
                .ConfigureAwait(false);
        }

        #endregion

        #region Error cases

        /// <summary>
        /// Non-numeric strings should cause arithmetic errors.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task NonNumericStringCausesArithmeticError(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            await Assert
                .That(() => script.DoString("return 'hello' + 1"))
                .Throws<ScriptRuntimeException>()
                .Because($"Non-numeric string arithmetic should throw in {version}")
                .ConfigureAwait(false);
        }

        #endregion

        #region Helpers

        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            return new Script(
                new ScriptOptions(Script.DefaultOptions) { CompatibilityVersion = version }
            );
        }

        #endregion
    }
}
