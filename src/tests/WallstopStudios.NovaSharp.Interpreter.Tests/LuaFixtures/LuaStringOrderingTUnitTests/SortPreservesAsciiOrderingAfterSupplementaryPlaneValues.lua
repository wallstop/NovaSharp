-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/LuaStringOrderingTUnitTests.cs:53
-- @test: LuaStringOrderingTUnitTests.SortPreservesAsciiOrderingAfterSupplementaryPlaneValues
-- Compatibility notes: Lua 5.3+: bitwise OR
local values = { 'z', '𐀀', 'A', '', '' }
                table.sort(values)
                local outcome = table.concat(values, '|')
                print(outcome)
                return outcome
