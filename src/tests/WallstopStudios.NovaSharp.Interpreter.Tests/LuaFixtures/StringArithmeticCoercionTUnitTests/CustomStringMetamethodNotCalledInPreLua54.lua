-- @lua-versions: 5.1, 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:269
-- @test: StringArithmeticCoercionTUnitTests.CustomStringMetamethodNotCalledInPreLua54
-- @compat-notes: Test targets Lua 5.1
local mt = getmetatable('')
                local called = false
                mt.__add = function(a, b)
                    called = true
                    return tonumber(a) + tonumber(b)
                end
                local sum = '10' + 1
                mt.__add = nil  -- cleanup
                return called, sum
