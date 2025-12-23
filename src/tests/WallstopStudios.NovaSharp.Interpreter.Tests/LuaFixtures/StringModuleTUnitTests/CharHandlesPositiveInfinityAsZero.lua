-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:447
-- @test: StringModuleTUnitTests.CharErrorsOnPositiveInfinity
-- Lua 5.1/5.2: Positive infinity throws "invalid value" error
-- Lua 5.3+: Throws "number has no integer representation"
return string.char(1/0)
