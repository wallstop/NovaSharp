-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaMathMultiVersionSpecTUnitTests.cs:91
-- @test: LuaMathMultiVersionSpecTUnitTests.MathToIntegerReturnsNilForFunction
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.tointeger (5.3+)
return math.tointeger(function() end)
