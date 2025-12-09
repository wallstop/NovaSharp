-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:471
-- @test: OverloadedMethodMemberDescriptorTUnitTests.VarArgsSingleArrayArgumentExactMatch
-- @compat-notes: Lua 5.3+: bitwise operators
local obj = TestClass.__new()
                return obj.WithVarArgs(1, 2, 3)
