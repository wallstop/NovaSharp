-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataFieldsTUnitTests.cs:329
-- @test: SomeClass.InteropIntFieldSetterWithSimplifiedSyntax
return myobj1.ObjProp, myobj2.ObjProp, myobj2.ObjProp.ObjProp;
