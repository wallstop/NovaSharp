-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\Lua55SpecTUnitTests.cs:84
-- @test: Lua55SpecTUnitTests.StringCharErrorsOnNonIntegerFloat
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(string.char, 65.5) return ok, err
