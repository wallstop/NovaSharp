-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Interop\Descriptors\EventMemberDescriptorTUnitTests.cs:458
-- @test: EventMemberDescriptorTUnitTests.DispatchEventForwardsMultipleArguments
-- @compat-notes: Lua 5.3+: bitwise operators
return function(a, b, c) payload = table.concat({a, b, c}, ":") end
