-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:125
-- @test: ClosureTUnitTests.DelegatesInvokeScriptFunction
return function(a, b) return a + b end
