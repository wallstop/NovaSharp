-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:574
-- @test: StringModuleTUnitTests.FormatUnsignedWithZeroPadding
return string.format('%08u', 42)
