-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataEventsTUnitTests.cs:263
-- @test: SomeClass.InteropSEventDetachAndDeregister
-- @compat-notes: Uses injected variable: myobj
myobj.MySEvent.remove(handler);
