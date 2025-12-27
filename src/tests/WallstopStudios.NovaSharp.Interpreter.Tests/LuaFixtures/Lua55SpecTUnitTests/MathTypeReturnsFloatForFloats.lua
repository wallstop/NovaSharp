-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:185
-- @test: Lua55SpecTUnitTests.MathTypeReturnsFloatForFloats
-- @compat-notes: Test targets Lua 5.5+; Lua 5.3+: math.type (5.3+)
return math.type(42.0)
