-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:379
-- @test: IoModuleTUnitTests.SetDefaultFileOverridesStdInStream
return io.read('*l')
