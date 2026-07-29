-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:84
-- @test: ClosureTUnitTests.ClosureCallObjectArgumentsSupportsCallerOwnedSpan
return function(...) return select('#', ...), ... end
