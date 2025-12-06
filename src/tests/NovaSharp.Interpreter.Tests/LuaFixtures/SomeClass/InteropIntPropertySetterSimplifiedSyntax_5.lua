-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataPropertiesTUnitTests.cs:375
-- @test: SomeClass.InteropIntPropertySetterSimplifiedSyntax
-- @compat-notes: Lua 5.3+: bitwise operators
myobj1.NIntProp = nil; myobj2.NIntProp = 19;
