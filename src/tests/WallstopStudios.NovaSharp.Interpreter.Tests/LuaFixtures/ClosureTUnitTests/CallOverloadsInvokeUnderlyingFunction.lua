-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:295
-- @test: ClosureTUnitTests.CallOverloadsInvokeUnderlyingFunction
return function(a, b, c, d, e) return (a or 0) + (b or 0) + (c or 0) + (d or 0) + (e or 0) end
