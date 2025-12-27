-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:215
-- @test: StringArithmeticCoercionTUnitTests.CustomStringMetamethodIsCalledInLua54Plus
-- @compat-notes: Test targets Lua 5.3+
local mt = getmetatable('')
                local original_add = mt.__add
                local called = false
                mt.__add = function(a, b)
                    called = true
                    return original_add(a, b)
                end
                local sum = '10' + 1
                mt.__add = original_add  -- restore original
                return called, sum
