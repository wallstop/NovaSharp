-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:282
-- @test: MathNumericEdgeCasesTUnitTests.FloatModuloByZeroReturnsNaNAllVersions
-- @compat-notes: Test targets Lua 5.1
return 5.0 % 0.0
