-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:350
-- @test: Lua55SpecTUnitTests.BitwiseOrOperatorWorks
-- @compat-notes: Lua 5.3+: bitwise OR
return 0xF0 | 0x0F
