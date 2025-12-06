-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:18
-- @test: StringModuleTUnitTests.CharProducesStringFromByteValues
return string.char(65, 66, 67)
