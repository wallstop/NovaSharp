-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:83
-- @test: IoModuleTUnitTests.OpenThrowsForInvalidMode
return io.open('{path}', 'z')
