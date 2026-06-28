-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Descriptors\OverloadedMethodMemberDescriptorTUnitTests.cs:268
-- @test: OverloadedMethodMemberDescriptorTUnitTests.OverloadResolutionThrowsWhenNoMatch
-- Test targets Lua 5.1
local obj = TestClass.__new()
                    return obj.WithInt('not a number', 'extra')
