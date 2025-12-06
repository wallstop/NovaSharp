-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:437
-- @test: StringModuleTUnitTests.CharHandlesNaNAsZero
return string.char(0/0)
