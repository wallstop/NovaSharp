-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/EventMemberDescriptorTUnitTests.cs:237
-- @test: EventMemberDescriptorTUnitTests.RemovingUnknownCallbackLeavesDelegateAttached
-- @compat-notes: Lua 5.3+: bitwise operators
return function(_, amount) {HitsVariable} = {HitsVariable} + amount end
