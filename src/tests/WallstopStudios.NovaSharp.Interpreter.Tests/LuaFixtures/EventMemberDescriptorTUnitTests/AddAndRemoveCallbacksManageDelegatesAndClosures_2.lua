-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/EventMemberDescriptorTUnitTests.cs:92
-- @test: EventMemberDescriptorTUnitTests.AddAndRemoveCallbacksManageDelegatesAndClosures
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return function(sender, arg) {HitsVariable} = {HitsVariable} + 10 end
