-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:492
-- @test: MathModuleTUnitTests.FrexpAndLdexpRoundTrip
-- Test targets Lua 5.3+
local m, e = math.frexp(123.456)
                return math.ldexp(m, e)
