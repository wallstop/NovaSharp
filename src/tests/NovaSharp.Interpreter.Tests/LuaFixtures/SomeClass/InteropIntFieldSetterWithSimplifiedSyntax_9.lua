-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataFieldsTUnitTests.cs:451
-- @test: SomeClass.InteropIntFieldSetterWithSimplifiedSyntax
-- @compat-notes: Uses injected variable: myobj
return myobj.ConstIntProp;
