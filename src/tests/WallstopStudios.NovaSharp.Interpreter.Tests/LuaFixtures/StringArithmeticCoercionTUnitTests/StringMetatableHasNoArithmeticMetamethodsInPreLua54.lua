-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:40
-- @test: StringArithmeticCoercionTUnitTests.StringMetatableHasNoArithmeticMetamethodsInPreLua54
-- @compat-notes: Test targets Lua 5.3+
local mt = getmetatable('')
                return mt and mt.__add or nil
