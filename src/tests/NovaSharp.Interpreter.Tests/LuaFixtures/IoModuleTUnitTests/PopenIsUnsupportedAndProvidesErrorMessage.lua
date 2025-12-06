-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:945
-- @test: IoModuleTUnitTests.PopenIsUnsupportedAndProvidesErrorMessage
return type(io.popen)
