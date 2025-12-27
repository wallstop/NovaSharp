-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:360
-- @test: Lua55SpecTUnitTests.BitwiseXorOperatorWorks
-- @compat-notes: Test targets Lua 5.5+; Lua 5.3+: bitwise XOR/NOT
return 0xFF ~ 0x0F
