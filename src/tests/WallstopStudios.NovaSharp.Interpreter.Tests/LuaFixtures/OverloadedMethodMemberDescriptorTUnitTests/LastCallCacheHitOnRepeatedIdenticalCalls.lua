-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:790
-- @test: OverloadedMethodMemberDescriptorTUnitTests.LastCallCacheHitOnRepeatedIdenticalCalls
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                local total = 0
                for i = 1, 100 do
                    total = total + obj.WithInt(i)
                end
                return total
