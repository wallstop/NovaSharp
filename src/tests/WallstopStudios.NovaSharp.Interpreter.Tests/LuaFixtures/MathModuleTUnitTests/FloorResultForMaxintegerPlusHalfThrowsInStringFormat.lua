-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:737
-- @test: MathModuleTUnitTests.FloorResultForMaxintegerPlusHalfThrowsInStringFormat
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.maxinteger (5.3+)
return string.format('%d', math.floor(math.maxinteger + 0.5))
