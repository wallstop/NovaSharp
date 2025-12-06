-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataFieldsTUnitTests.cs:307
-- @test: SomeClass.InteropIntFieldSetterWithSimplifiedSyntax
return myobj1.NIntProp, myobj2.NIntProp;
