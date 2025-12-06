-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/JsonModuleTUnitTests.cs:64
-- @test: JsonModuleTUnitTests.ParseThrowsScriptRuntimeExceptionOnInvalidJson
return require('json')
