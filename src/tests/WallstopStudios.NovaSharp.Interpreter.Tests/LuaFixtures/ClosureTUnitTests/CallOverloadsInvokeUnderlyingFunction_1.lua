-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:352
-- @test: ClosureTUnitTests.CallOverloadsInvokeUnderlyingFunction
return function(value) return value end
