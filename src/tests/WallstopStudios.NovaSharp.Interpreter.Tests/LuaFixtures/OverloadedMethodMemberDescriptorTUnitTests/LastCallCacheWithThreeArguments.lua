-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:938
-- @test: OverloadedMethodMemberDescriptorTUnitTests.LastCallCacheWithThreeArguments
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                local total = 0
                for i = 1, 10 do
                    total = total + obj.ThreeArgs(1, 2, 3)
                end
                return total
