-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/EventMemberDescriptorTUnitTests.cs:267
-- @test: EventMemberDescriptorTUnitTests.AddingSameClosureTwiceDoesNotRegisterDuplicateDelegates
return function() end
