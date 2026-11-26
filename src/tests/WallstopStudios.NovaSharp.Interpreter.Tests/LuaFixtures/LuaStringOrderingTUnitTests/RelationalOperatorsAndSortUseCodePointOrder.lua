-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/LuaStringOrderingTUnitTests.cs:19
-- @test: LuaStringOrderingTUnitTests.RelationalOperatorsAndSortUseCodePointOrder
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
