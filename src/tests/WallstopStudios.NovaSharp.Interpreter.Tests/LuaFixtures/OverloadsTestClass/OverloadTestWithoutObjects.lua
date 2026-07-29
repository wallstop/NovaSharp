-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataOverloadsTUnitTests.cs:398
-- @test: OverloadsTestClass.OverloadTestWithoutObjects
-- Compatibility notes: Uses injected variable: func
return func(), func(17)
