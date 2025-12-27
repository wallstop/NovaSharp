-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:492
-- @test: MathModuleTUnitTests.FrexpAndLdexpRoundTrip
-- @compat-notes: Test targets Lua 5.3+
local m, e = math.frexp(123.456)
                return math.ldexp(m, e)
