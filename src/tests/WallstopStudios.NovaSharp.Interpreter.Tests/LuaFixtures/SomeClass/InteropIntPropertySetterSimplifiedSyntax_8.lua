-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\UserDataPropertiesTUnitTests.cs:442
-- @test: SomeClass.InteropIntPropertySetterSimplifiedSyntax
-- Uses injected variable: static
static.StaticProp = 'asdasd' .. static.StaticProp;
