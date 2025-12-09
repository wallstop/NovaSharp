-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/DispatchingUserDataDescriptorTUnitTests.cs:123
-- @test: DispatchingUserDataDescriptorTUnitTests.LenOperatorFallsBackToCountWhenLengthMissing
return #countOnly
