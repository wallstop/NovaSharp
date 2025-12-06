-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataFieldsTUnitTests.cs:368
-- @test: SomeClass.InteropIntFieldSetterWithSimplifiedSyntax
-- @compat-notes: Lua 5.3+: bitwise operators
myobj1.NIntProp = nil; myobj2.NIntProp = 19;
