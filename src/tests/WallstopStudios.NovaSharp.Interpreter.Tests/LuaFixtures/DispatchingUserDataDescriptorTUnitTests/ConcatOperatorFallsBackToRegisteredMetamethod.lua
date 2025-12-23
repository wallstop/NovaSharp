-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/DispatchingUserDataDescriptorTUnitTests.cs:447
-- @test: DispatchingUserDataDescriptorTUnitTests.ConcatOperatorFallsBackToRegisteredMetamethod
return concatLeft .. concatRight
