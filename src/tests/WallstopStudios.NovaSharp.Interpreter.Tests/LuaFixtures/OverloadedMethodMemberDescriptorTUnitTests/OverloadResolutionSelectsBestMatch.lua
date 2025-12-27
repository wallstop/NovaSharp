-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:245
-- @test: OverloadedMethodMemberDescriptorTUnitTests.OverloadResolutionSelectsBestMatch
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                return obj.WithInt(42)
