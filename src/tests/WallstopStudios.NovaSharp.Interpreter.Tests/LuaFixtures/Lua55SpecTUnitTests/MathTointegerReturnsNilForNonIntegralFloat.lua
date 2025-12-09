-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\Lua55SpecTUnitTests.cs:152
-- @test: Lua55SpecTUnitTests.MathTointegerReturnsNilForNonIntegralFloat
-- @compat-notes: Lua 5.3+: math.tointeger (5.3+)
return math.tointeger(42.5)
