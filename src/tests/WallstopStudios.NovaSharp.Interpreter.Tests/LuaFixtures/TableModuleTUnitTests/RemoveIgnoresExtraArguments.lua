-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:208
-- @test: TableModuleTUnitTests.RemoveIgnoresExtraArguments
-- @compat-notes: Test targets Lua 5.1
local values = { 1, 2, 3, 4, 5 }
                local removed = table.remove(values, 1, 'extra', 'args', 999)
                return removed, #values, values[1]
