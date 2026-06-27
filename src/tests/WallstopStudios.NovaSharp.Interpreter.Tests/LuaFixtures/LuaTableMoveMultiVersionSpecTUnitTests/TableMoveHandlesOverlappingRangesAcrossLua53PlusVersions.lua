-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaTableMoveMultiVersionSpecTUnitTests.cs:61
-- @test: LuaTableMoveMultiVersionSpecTUnitTests.TableMoveHandlesOverlappingRangesAcrossLua53PlusVersions
-- Test targets Lua 5.3+; Lua 5.3+: bitwise operators; Lua 5.3+: table.move
local values = { 1, 2, 3, 4 }
                    table.move(values, 1, 3, 2)
                    return table.concat(values, ',')
