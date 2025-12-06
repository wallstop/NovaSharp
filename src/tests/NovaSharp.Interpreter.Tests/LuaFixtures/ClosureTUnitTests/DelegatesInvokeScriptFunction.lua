-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ClosureTUnitTests.cs:112
-- @test: ClosureTUnitTests.DelegatesInvokeScriptFunction
return function(a, b) return a + b end
