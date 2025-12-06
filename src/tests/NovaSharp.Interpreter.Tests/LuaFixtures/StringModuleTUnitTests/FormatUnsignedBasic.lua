-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:556
-- @test: StringModuleTUnitTests.FormatUnsignedBasic
return string.format('%u', 42)
