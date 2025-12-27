-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:391
-- @test: Lua55SpecTUnitTests.RightShiftOperatorWorks
-- @compat-notes: Test targets Lua 5.5+; Lua 5.3+: bit shift
return 32 >> 2
