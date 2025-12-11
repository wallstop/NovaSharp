-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.CharErrorsOnNegativeInfinityLua53Plus
-- Lua 5.3+: Negative infinity throws "number has no integer representation"
return string.char(-1/0)
