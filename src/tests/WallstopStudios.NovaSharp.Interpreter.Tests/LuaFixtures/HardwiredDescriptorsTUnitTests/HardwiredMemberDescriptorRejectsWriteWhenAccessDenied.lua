-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/HardwiredDescriptorsTUnitTests.cs:40
-- @test: HardwiredDescriptorsTUnitTests.HardwiredMemberDescriptorRejectsWriteWhenAccessDenied
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: obj
obj.readonly = 'x'
