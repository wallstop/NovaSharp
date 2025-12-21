-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataPropertiesTUnitTests.cs:314
-- @test: SomeClass.InteropIntPropertySetterSimplifiedSyntax
return myobj1.NIntProp, myobj2.NIntProp;
