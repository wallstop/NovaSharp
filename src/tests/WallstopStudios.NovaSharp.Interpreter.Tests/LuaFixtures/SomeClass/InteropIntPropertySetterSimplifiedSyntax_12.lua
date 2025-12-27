-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataPropertiesTUnitTests.cs:513
-- @test: SomeClass.InteropIntPropertySetterSimplifiedSyntax
-- @compat-notes: Uses injected variable: myobj
myobj.RoIntProp = 1; return myobj.RoIntProp;
