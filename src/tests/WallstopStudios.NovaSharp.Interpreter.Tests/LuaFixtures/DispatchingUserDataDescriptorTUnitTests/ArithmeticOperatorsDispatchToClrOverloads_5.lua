-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Descriptors\DispatchingUserDataDescriptorTUnitTests.cs:44
-- @test: DispatchingUserDataDescriptorTUnitTests.ArithmeticOperatorsDispatchToClrOverloads
return (-hostAdd).value
