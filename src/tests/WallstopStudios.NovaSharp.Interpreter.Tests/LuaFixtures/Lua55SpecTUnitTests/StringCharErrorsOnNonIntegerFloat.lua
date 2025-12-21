-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:89
-- @test: Lua55SpecTUnitTests.StringCharErrorsOnNonIntegerFloat
local ok, err = pcall(string.char, 65.5) return ok, err
