-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:489
-- @test: Lua55SpecTUnitTests.TypeFunctionReturnsCorrectTypes
return type(nil), type(true), type(42), type('str'), type({})
