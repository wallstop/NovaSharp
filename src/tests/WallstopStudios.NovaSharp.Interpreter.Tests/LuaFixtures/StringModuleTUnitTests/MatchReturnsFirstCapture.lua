-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:248
-- @test: StringModuleTUnitTests.MatchReturnsFirstCapture
return string.match('Version: 1.2.3', '%d+%.%d+%.%d+')
