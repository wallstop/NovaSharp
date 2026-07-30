-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataPropertiesTUnitTests.cs:375
-- @test: SomeClass.InteropIntPropertySetterSimplifiedSyntax
myobj1.NIntProp = nil; myobj2.NIntProp = 19;
