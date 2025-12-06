-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Descriptors/DispatchingUserDataDescriptorTUnitTests.cs:39
-- @test: DispatchingUserDataDescriptorTUnitTests.ArithmeticOperatorsDispatchToClrOverloads
return (hostAdd + hostOther).value
