-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\UserDataPropertiesTUnitTests.cs:492
-- @test: SomeClass.InteropIntPropertySetterSimplifiedSyntax
-- @compat-notes: Uses injected variable: myobj
return myobj.RoIntProp;
