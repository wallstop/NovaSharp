namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Locks Lua's byte-order string comparison semantics for relational operators and sorting.
    /// </summary>
    public sealed class LuaStringOrderingTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RelationalOperatorsAndSortUseCodePointOrder(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"
                local bmp = ''
                local astral = '𐀀'

                local values = { astral, bmp }
                table.sort(values)

                return table.concat({
                    tostring(bmp < astral),
                    tostring(bmp <= astral),
                    tostring(astral < bmp),
                    tostring(values[1] == bmp),
                }, ',')
            "
            );

            await Assert
                .That(result.String)
                .IsEqualTo("true,true,false,true")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RawBytesSortAfterSupplementaryPlaneLeadBytes(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"
                local raw = string.char(255)
                local astral = '𐀀'
                local values = { raw, astral }
                table.sort(values)

                return table.concat({
                    tostring(raw < astral),
                    tostring(astral < raw),
                    tostring(values[1] == astral),
                }, ',')
            "
            );

            await Assert.That(result.String).IsEqualTo("false,true,true").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SortPreservesAsciiOrderingAfterSupplementaryPlaneValues(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            LuaValue result = script.DoString(
                @"
                local values = { 'z', '𐀀', 'A', '', '' }
                table.sort(values)
                return table.concat(values, '|')
            "
            );

            await Assert.That(result.String).IsEqualTo("|A|z||𐀀").ConfigureAwait(false);
        }
    }
}
