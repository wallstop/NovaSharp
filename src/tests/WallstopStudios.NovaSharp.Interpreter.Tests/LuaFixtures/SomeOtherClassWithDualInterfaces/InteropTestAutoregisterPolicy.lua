-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMethodsTUnitTests.cs:1114
-- @test: SomeOtherClassWithDualInterfaces.InteropTestAutoregisterPolicy
-- @compat-notes: Uses injected variable: myobj
return myobj:Test1()
