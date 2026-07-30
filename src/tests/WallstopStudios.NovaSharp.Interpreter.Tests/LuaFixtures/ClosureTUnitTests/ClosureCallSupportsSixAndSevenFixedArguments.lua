-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:42
-- @test: ClosureTUnitTests.ClosureCallSupportsSixAndSevenFixedArguments
return function(...) return select('#', ...), ... end
