-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\Lua55SpecTUnitTests.cs:343
-- @test: Lua55SpecTUnitTests.BitwiseNotOperatorWorks
-- @compat-notes: Lua 5.3+: bitwise AND; Lua 5.3+: bitwise XOR/NOT
return (~0) & 0xFFFFFFFF
