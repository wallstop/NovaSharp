-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\UserDataPropertiesTUnitTests.cs:509
-- @test: SomeClass.InteropIntPropertySetterSimplifiedSyntax
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: myobj
myobj.RoIntProp = 1; return myobj.RoIntProp;
