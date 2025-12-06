-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataFieldsTUnitTests.cs:350
-- @test: SomeClass.InteropIntFieldSetterWithSimplifiedSyntax
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: myobj
myobj.IntProp = 19;
