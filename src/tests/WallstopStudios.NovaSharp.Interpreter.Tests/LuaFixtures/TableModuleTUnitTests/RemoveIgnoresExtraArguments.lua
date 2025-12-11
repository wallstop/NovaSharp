-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:188
-- @test: TableModuleTUnitTests.RemoveIgnoresExtraArguments
-- @compat-notes: Lua 5.3+: bitwise operators
local values = { 1, 2, 3, 4, 5 }
                local removed = table.remove(values, 1, 'extra', 'args', 999)
                return removed, #values, values[1]
