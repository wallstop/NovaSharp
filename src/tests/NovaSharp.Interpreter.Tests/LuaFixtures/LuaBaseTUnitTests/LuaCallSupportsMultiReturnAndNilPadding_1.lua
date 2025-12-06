-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/LuaBaseTUnitTests.cs:258
-- @test: LuaBaseTUnitTests.LuaCallSupportsMultiReturnAndNilPadding
return function(value) return value end
