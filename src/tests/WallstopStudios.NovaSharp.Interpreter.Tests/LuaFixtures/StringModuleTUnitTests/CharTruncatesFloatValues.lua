-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:88
-- @test: StringModuleTUnitTests.CharTruncatesFloatValues
-- Lua 5.1/5.2: Non-integer floats are truncated (65.5 → 65)
-- Lua 5.3+: Throws "number has no integer representation"
return string.char(65.5)
