-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:135
-- @test: ClosureTUnitTests.CallOverloadsInvokeUnderlyingFunction
return function(a, b) return (a or 0) + (b or 0) end
