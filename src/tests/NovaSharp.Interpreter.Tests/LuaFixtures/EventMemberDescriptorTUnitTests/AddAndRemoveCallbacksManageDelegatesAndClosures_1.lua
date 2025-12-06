-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/EventMemberDescriptorTUnitTests.cs:89
-- @test: EventMemberDescriptorTUnitTests.AddAndRemoveCallbacksManageDelegatesAndClosures
-- @compat-notes: Lua 5.3+: bitwise operators
return function(sender, arg) {HitsVariable} = {HitsVariable} + 1 end
