-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/EventMemberDescriptorTUnitTests.cs:433
-- @test: EventMemberDescriptorTUnitTests.DispatchEventInvokesZeroArgumentHandlers
-- @compat-notes: Lua 5.3+: bitwise operators
return function() hits = hits + 1 end
