-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:519
-- @test: IoModuleTUnitTests.TypeReturnsNilForNonUserDataArguments
return io.Type(42), io.Type({})
