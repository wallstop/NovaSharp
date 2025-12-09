-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Interop\Descriptors\HardwiredDescriptorsTUnitTests.cs:27
-- @test: HardwiredDescriptorsTUnitTests.HardwiredMemberDescriptorSupportsReadAndWrite
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: obj
obj.value = 123
return obj.value
