namespace NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Spec harness for Lua basic library behaviours (§6.1), currently focused on tonumber base parsing.
    /// </summary>
    public sealed class LuaBasicMultiVersionSpecTUnitTests : LuaSpecTestBase
    {
        [global::TUnit.Core.Test]
        public async Task ToNumberParsesIntegersAcrossSupportedBases()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModules.PresetComplete
            );
            DynValue tuple = script.DoString(
                @"
                return tonumber('1010', 2),
                       tonumber('-77', 8),
                       tonumber('+1e', 16),
                       tonumber('Z', 36)
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(10);
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(-63);
            await Assert.That(tuple.Tuple[2].Number).IsEqualTo(30);
            await Assert.That(tuple.Tuple[3].Number).IsEqualTo(35);
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberReturnsNilWhenDigitsExceedBase()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModules.PresetComplete
            );
            DynValue tuple = script.DoString("return tonumber('2', 2), tonumber('g', 16)");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberErrorsWhenBaseIsOutOfRange()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModules.PresetComplete
            );
            DynValue result = script.DoString(
                "local ok, err = pcall(tonumber, '1', 40) return ok, err"
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).Contains("base out of range");
        }

        [global::TUnit.Core.Test]
        public async Task ToNumberErrorsWhenBaseIsFractional()
        {
            Script script = CreateScript(
                LuaCompatibilityVersion.Latest,
                CoreModules.PresetComplete
            );
            DynValue result = script.DoString(
                "local ok, err = pcall(tonumber, '10', 2.5) return ok, err"
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).Contains("integer");
        }
    }
}
