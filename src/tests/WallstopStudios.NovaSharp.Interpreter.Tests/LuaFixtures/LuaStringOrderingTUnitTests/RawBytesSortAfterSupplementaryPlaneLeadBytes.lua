-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/LuaStringOrderingTUnitTests.cs:51
-- @test: LuaStringOrderingTUnitTests.RawBytesSortAfterSupplementaryPlaneLeadBytes
local raw = string.char(255)
                local astral = '𐀀'
                local values = { raw, astral }
                table.sort(values)

                return table.concat({
                    tostring(raw < astral),
                    tostring(astral < raw),
                    tostring(values[1] == astral),
                }, ',')
