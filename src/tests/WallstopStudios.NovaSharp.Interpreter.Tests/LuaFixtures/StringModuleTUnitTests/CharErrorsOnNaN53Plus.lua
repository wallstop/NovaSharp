-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.CharErrorsOnNaNLua53Plus
-- Lua 5.3+: NaN throws "number has no integer representation"
return string.char(0/0)
