-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataPropertiesTUnitTests.cs:441
-- @test: SomeClass.InteropIntPropertySetterSimplifiedSyntax
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: static
static.StaticProp = 'asdasd' .. static.StaticProp;
