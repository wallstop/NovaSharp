-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/LuaBaseTUnitTests.cs:243
-- @test: LuaBaseTUnitTests.LuaCallSupportsMultiReturnAndNilPadding
return function(input) return 1, 2 end
