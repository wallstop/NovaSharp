-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathIntegerFunctionsTUnitTests.cs:479
-- @test: MathIntegerFunctionsTUnitTests.ModuloByZeroThrows
return 1 % 0
