-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:489
-- @test: ClosureTUnitTests.ObjectArrayCallPreservesSpreadAndSingleObjectForms
return function(...) return select('#', ...), type((...)), ... end
