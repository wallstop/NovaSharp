-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/HardwiredDescriptorsTUnitTests.cs:71
-- @test: HardwiredDescriptorsTUnitTests.HardwiredMethodDescriptorAppliesDefaultArguments
-- @compat-notes: Uses injected variable: obj
return obj:call(5)
