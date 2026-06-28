-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:717
-- @test: MathModuleTUnitTests.FloorResultCanBeUsedInStringFormat
-- Test targets Lua 5.3+
return string.format('%d', math.floor(3.7))
