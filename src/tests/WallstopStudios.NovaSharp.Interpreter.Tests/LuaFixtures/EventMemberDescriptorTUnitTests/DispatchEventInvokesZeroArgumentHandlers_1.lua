-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Interop\Descriptors\EventMemberDescriptorTUnitTests.cs:433
-- @test: EventMemberDescriptorTUnitTests.DispatchEventInvokesZeroArgumentHandlers
-- @compat-notes: Lua 5.3+: bitwise operators
return function() hits = hits + 1 end
