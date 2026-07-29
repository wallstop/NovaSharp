-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataPropertiesTUnitTests.cs:397
-- @test: SomeClass.InteropIntPropertySetterSimplifiedSyntax
myobj1.ObjProp = myobj2; myobj2.ObjProp = 'hello';
