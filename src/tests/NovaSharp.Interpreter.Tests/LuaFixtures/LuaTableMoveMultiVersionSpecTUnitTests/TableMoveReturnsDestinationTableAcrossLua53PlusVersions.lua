-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Spec/LuaTableMoveMultiVersionSpecTUnitTests.cs:39
-- @test: LuaTableMoveMultiVersionSpecTUnitTests.TableMoveReturnsDestinationTableAcrossLua53PlusVersions
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: bitwise operators; Lua 5.3+: table.move
local src = { 'a', 'b', 'c' }
                    local dest = {}
                    local returned = table.move(src, 1, #src, 1, dest)
                    return dest[1], dest[2], dest[3], returned == dest
