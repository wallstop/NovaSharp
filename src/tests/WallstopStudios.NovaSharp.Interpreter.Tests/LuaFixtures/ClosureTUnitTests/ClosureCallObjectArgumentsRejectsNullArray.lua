-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:170
-- @test: ClosureTUnitTests.ClosureCallObjectArgumentsRejectsNullArray
return function(...) return select('#', ...) end
