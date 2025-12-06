-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ClosureTUnitTests.cs:129
-- @test: ClosureTUnitTests.CallOverloadsInvokeUnderlyingFunction
return function(a, b) return (a or 0) + (b or 0) end
