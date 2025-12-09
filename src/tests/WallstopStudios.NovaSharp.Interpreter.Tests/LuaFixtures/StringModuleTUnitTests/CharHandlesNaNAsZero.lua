-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:437
-- @test: StringModuleTUnitTests.CharHandlesNaNAsZero
-- Lua 5.1/5.2: NaN is treated as 0, producing a null byte
-- Lua 5.3+: Throws "number has no integer representation"
return string.char(0/0)
