-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:492
-- @test: MathModuleTUnitTests.FrexpAndLdexpRoundTrip
-- @compat-notes: Platform-specific: Windows Lua built without LUA_COMPAT_MATHLIB. NovaSharp provides deprecated math functions for compatibility.
local m, e = math.frexp(123.456)
return math.ldexp(m, e)