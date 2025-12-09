-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\SimpleTUnitTests.cs:1635
-- @test: SimpleTUnitTests.NumericConversionFailsIfOutOfBounds
my_function_takes_byte(2010191) -- a huge number that is definitely not a byte
