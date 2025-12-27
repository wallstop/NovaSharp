-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:340
-- @test: Lua55SpecTUnitTests.BitwiseAndOperatorWorks
-- @compat-notes: Test targets Lua 5.5+; Lua 5.3+: bitwise AND
return 0xFF & 0x0F
