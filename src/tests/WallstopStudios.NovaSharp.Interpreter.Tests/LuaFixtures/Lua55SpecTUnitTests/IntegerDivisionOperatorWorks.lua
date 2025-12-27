-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:330
-- @test: Lua55SpecTUnitTests.IntegerDivisionOperatorWorks
-- @compat-notes: Test targets Lua 5.5+; Lua 5.3+: floor division
return 7 // 3
