-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/EventMemberDescriptorTUnitTests.cs:240
-- @test: EventMemberDescriptorTUnitTests.RemovingSameCallbackTwiceDoesNotDetachDelegateAgain
return function() end
