-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Descriptors/HardwiredDescriptorTUnitTests.cs:342
-- @test: HardwiredDescriptorTUnitTests.DynValueMemberDescriptorPrepareForWiringHandlesUnsupportedTypes
return function() return 1 end
