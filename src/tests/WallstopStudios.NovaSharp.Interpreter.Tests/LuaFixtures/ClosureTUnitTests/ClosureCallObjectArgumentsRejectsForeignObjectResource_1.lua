-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:190
-- @test: ClosureTUnitTests.ClosureCallObjectArgumentsRejectsForeignObjectResource
return function(value) return value end
