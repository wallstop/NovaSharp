-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:123
-- @test: ClosureTUnitTests.ClosureCallObjectArgumentsPreservesArrayApiShape
return function(...) return select('#', ...), type((...)), ... end
