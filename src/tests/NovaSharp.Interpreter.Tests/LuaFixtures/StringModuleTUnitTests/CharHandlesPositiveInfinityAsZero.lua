-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:447
-- @test: StringModuleTUnitTests.CharHandlesPositiveInfinityAsZero
return string.char(1/0)
