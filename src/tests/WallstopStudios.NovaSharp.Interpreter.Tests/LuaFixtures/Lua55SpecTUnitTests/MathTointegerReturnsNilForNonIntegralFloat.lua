-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:165
-- @test: Lua55SpecTUnitTests.MathTointegerReturnsNilForNonIntegralFloat
-- @compat-notes: Test targets Lua 5.5+; Lua 5.3+: math.tointeger (5.3+)
return math.tointeger(42.5)
