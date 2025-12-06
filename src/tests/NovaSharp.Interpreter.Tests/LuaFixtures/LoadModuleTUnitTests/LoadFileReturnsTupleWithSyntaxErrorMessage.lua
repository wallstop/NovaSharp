-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:237
-- @test: LoadModuleTUnitTests.LoadFileReturnsTupleWithSyntaxErrorMessage
return loadfile('broken.lua')
