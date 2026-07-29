-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:273
-- @test: ClosureTUnitTests.DelegatesInvokeScriptFunction
-- Compatibility notes: Test targets Lua 5.1
return function(a, b) return a + b end
