-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Descriptors\OverloadedMethodMemberDescriptorTUnitTests.cs:201
-- @test: OverloadedMethodMemberDescriptorTUnitTests.OverloadResolutionThrowsWhenNoMatch
-- @compat-notes: Lua 5.3+: bitwise operators
local obj = TestClass.__new()
                    return obj.WithInt('not a number', 'extra')
