-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:376
-- @test: MathModuleTUnitTests.FrexpWithZeroReturnsZeroMantissaAndExponent
-- @compat-notes: Platform-specific: Windows Lua built without LUA_COMPAT_MATHLIB. NovaSharp provides deprecated math functions for compatibility.
return math.frexp(0)