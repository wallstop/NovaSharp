-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:510
-- @test: IoModuleTUnitTests.TypeReturnsNilForNonUserData
return io.Type(123)
