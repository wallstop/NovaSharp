-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:381
-- @test: Lua55SpecTUnitTests.LeftShiftOperatorWorks
-- @compat-notes: Test targets Lua 5.5+; Lua 5.3+: bit shift
return 1 << 4
