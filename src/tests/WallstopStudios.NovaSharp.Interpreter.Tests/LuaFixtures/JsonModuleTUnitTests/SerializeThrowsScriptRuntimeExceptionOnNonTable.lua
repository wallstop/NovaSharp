-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/JsonModuleTUnitTests.cs:77
-- @test: JsonModuleTUnitTests.SerializeThrowsScriptRuntimeExceptionOnNonTable
return require('json')
