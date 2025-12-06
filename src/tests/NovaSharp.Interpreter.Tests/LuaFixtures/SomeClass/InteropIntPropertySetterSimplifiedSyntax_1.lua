-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataPropertiesTUnitTests.cs:295
-- @test: SomeClass.InteropIntPropertySetterSimplifiedSyntax
-- @compat-notes: Uses injected variable: myobj
return myobj.IntProp;
