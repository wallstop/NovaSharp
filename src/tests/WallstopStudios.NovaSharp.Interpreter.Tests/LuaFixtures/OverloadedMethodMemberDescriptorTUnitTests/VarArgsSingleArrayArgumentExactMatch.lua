-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:611
-- @test: OverloadedMethodMemberDescriptorTUnitTests.VarArgsSingleArrayArgumentExactMatch
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                return obj.WithVarArgs(1, 2, 3)
