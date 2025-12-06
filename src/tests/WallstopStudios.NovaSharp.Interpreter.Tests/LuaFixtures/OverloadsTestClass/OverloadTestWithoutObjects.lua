-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataOverloadsTUnitTests.cs:396
-- @test: OverloadsTestClass.OverloadTestWithoutObjects
return func(), func(17)
