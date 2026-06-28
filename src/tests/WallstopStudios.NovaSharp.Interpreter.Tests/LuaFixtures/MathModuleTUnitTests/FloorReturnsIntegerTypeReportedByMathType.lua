-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:550
-- @test: MathModuleTUnitTests.FloorReturnsIntegerTypeReportedByMathType
-- Test targets Lua 5.3+; Lua 5.3+: math.type (5.3+)
return math.type(math.floor(3.7))
