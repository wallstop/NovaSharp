-- @lua-versions: 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1390
-- @test: MathModuleTUnitTests.Log10ReturnsCorrectValues
-- @compat-notes: Platform-specific: Windows Lua built without LUA_COMPAT_MATHLIB. NovaSharp provides deprecated math functions for compatibility.
return math.log10(1000)