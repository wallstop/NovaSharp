-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Descriptors/DispatchingUserDataDescriptorTUnitTests.cs:373
-- @test: DispatchingUserDataDescriptorTUnitTests.IndexFallsBackToExtensionMethodsAfterRegistration
return hostAdd:DescribeExt()
