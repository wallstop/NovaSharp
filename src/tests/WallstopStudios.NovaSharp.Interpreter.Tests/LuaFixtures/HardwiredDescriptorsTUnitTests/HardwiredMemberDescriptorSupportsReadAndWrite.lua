-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Interop\Descriptors\HardwiredDescriptorsTUnitTests.cs:34
-- @test: HardwiredDescriptorsTUnitTests.HardwiredMemberDescriptorSupportsReadAndWrite
-- Uses injected variable: obj
obj.value = 123
return obj.value
