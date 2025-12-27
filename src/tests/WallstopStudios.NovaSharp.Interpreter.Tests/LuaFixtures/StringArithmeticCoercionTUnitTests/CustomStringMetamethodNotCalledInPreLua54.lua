-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:253
-- @test: StringArithmeticCoercionTUnitTests.CustomStringMetamethodNotCalledInPreLua54
-- @compat-notes: Test targets Lua 5.3+
local mt = getmetatable('')
                local called = false
                mt.__add = function(a, b)
                    called = true
                    return tonumber(a) + tonumber(b)
                end
                local sum = '10' + 1
                mt.__add = nil  -- cleanup
                return called, sum
