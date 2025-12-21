-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataFieldsTUnitTests.cs:368
-- @test: SomeClass.InteropIntFieldSetterWithSimplifiedSyntax
myobj1.NIntProp = nil; myobj2.NIntProp = 19;
