-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:42
-- @test: StringArithmeticCoercionTUnitTests.StringMetatableHasNoArithmeticMetamethodsInPreLua54
-- @compat-notes: Test targets Lua 5.1
local mt = getmetatable('')
                return mt and mt.__add or nil
