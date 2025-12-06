-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:485
-- @test: IoModuleTUnitTests.OpenThrowsWhenModeEmpty
return io.open('{path}', "")
