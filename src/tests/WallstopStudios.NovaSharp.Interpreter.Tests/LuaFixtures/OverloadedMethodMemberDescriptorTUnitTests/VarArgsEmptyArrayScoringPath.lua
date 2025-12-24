-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:588
-- @test: OverloadedMethodMemberDescriptorTUnitTests.VarArgsEmptyArrayScoringPath
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                return obj.WithVarArgs()
