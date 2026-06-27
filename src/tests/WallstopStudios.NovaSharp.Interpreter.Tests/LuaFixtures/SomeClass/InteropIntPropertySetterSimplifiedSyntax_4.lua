-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\UserDataPropertiesTUnitTests.cs:357
-- @test: SomeClass.InteropIntPropertySetterSimplifiedSyntax
-- Uses injected variable: myobj
myobj.IntProp = 19;
