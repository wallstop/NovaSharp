-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:183
-- @test: Lua55SpecTUnitTests.TableMoveShiftsElementsCorrectly
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.3+: table.move
local t = {1, 2, 3, 4, 5}
                table.move(t, 2, 4, 1)
                return t[1], t[2], t[3]
