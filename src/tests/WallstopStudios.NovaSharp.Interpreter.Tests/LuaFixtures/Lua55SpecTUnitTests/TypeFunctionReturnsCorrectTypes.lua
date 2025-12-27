-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:489
-- @test: Lua55SpecTUnitTests.TypeFunctionReturnsCorrectTypes
-- @compat-notes: Test targets Lua 5.5+
return type(nil), type(true), type(42), type('str'), type({})
