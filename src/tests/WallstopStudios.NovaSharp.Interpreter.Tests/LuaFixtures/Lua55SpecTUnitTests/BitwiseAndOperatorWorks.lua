-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\Lua55SpecTUnitTests.cs:315
-- @test: Lua55SpecTUnitTests.BitwiseAndOperatorWorks
-- @compat-notes: Lua 5.3+: bitwise AND
return 0xFF & 0x0F
