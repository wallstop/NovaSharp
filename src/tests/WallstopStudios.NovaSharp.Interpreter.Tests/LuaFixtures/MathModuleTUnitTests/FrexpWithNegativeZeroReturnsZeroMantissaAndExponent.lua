-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:394
-- @test: MathModuleTUnitTests.FrexpWithNegativeZeroReturnsZeroMantissaAndExponent
-- @compat-notes: Platform-specific: Windows Lua built without LUA_COMPAT_MATHLIB. NovaSharp provides deprecated math functions for compatibility.
return math.frexp(-0.0)