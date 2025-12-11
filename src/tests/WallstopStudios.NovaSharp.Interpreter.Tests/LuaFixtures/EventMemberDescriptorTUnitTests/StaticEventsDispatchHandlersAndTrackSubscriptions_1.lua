-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Interop\Descriptors\EventMemberDescriptorTUnitTests.cs:144
-- @test: EventMemberDescriptorTUnitTests.StaticEventsDispatchHandlersAndTrackSubscriptions
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return function(_, amount) {HitsVariable} = {HitsVariable} + amount end
