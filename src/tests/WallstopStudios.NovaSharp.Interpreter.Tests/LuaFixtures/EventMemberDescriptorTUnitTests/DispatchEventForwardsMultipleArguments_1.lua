-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/EventMemberDescriptorTUnitTests.cs:488
-- @test: EventMemberDescriptorTUnitTests.DispatchEventForwardsMultipleArguments
return function(a, b, c) payload = table.concat({a, b, c}, ":") end
