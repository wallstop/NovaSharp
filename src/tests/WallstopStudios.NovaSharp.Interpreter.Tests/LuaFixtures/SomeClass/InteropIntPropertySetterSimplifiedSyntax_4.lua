-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataPropertiesTUnitTests.cs:357
-- @test: SomeClass.InteropIntPropertySetterSimplifiedSyntax
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: myobj
myobj.IntProp = 19;
