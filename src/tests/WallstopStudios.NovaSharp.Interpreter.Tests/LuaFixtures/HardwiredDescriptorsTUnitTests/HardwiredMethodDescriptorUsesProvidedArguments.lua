-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/HardwiredDescriptorsTUnitTests.cs:123
-- @test: HardwiredDescriptorsTUnitTests.HardwiredMethodDescriptorUsesProvidedArguments
-- @compat-notes: Uses injected variable: obj
return obj:call(7, 'custom')
