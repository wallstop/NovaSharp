-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:834
-- @test: OverloadedMethodMemberDescriptorTUnitTests.ExtraArgumentsPenaltyApplied
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                return obj.WithInt(42, 'extra', 'arguments')
