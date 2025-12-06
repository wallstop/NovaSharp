-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/EventMemberDescriptorTUnitTests.cs:52
-- @test: EventMemberDescriptorTUnitTests.RemoveCallbackWithoutExistingSubscriptionDoesNotUnregister
return function() end
