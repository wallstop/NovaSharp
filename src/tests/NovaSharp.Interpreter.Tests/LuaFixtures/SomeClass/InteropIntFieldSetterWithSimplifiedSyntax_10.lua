-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataFieldsTUnitTests.cs:468
-- @test: SomeClass.InteropIntFieldSetterWithSimplifiedSyntax
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: myobj
myobj.ConstIntProp = 1; return myobj.ConstIntProp;
