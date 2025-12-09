-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaTableMoveMultiVersionSpecTUnitTests.cs:79
-- @test: LuaTableMoveMultiVersionSpecTUnitTests.TableMoveDefaultsDestinationToSourceAcrossLua53PlusVersions
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: bitwise operators; Lua 5.3+: table.move
local values = { 0, 0, 3, 4 }
                    table.move(values, 3, 4, 1)
                    return values[1], values[2], values[3], values[4]
