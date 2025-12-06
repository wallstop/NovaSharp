-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataFieldsTUnitTests.cs:485
-- @test: SomeClass.InteropIntFieldSetterWithSimplifiedSyntax
-- @compat-notes: Uses injected variable: myobj
return myobj.RoIntProp;
