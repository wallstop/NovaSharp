-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/EventMemberDescriptorTUnitTests.cs:212
-- @test: EventMemberDescriptorTUnitTests.RemovingSameCallbackTwiceDoesNotDetachDelegateAgain
return function() end
