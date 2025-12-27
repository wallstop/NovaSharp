-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/HardwiredDescriptorsTUnitTests.cs:53
-- @test: HardwiredDescriptorsTUnitTests.HardwiredMemberDescriptorRejectsWriteWhenAccessDenied
-- @compat-notes: Uses injected variable: obj
obj.readonly = 'x'
