-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.CharErrorsOnNonIntegerFloatLua53Plus
-- Lua 5.3+: Non-integer floats throw "number has no integer representation"
return string.char(65.5)
