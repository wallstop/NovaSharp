-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:71
-- @test: StringArithmeticCoercionTUnitTests.StringMetatableHasArithmeticMetamethodsInLua54Plus
-- @compat-notes: Test targets Lua 5.4+
local mt = getmetatable('')
                local hasAll = mt and
                    mt.__add and
                    mt.__sub and
                    mt.__mul and
                    mt.__div and
                    mt.__mod and
                    mt.__pow and
                    mt.__idiv and
                    mt.__unm
                return hasAll ~= nil
