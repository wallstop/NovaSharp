-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/EventMemberDescriptorTUnitTests.cs:267
-- @test: EventMemberDescriptorTUnitTests.AddingSameClosureTwiceDoesNotRegisterDuplicateDelegates
return function() end
