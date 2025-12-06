-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:165
-- @test: LoadModuleTUnitTests.LoadReturnsTupleWithSyntaxErrorWhenStringIsInvalid
return load('function(')
