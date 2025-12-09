-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataEventsTUnitTests.cs:316
-- @test: SomeClass.InteropSEventDetachAndReregister
-- @compat-notes: Uses injected variable: myobj
myobj.MySEvent.remove(handler);
