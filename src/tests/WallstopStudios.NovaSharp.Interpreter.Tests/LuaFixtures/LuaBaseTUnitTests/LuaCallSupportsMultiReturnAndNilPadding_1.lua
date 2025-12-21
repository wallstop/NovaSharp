-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LuaBaseTUnitTests.cs:258
-- @test: LuaBaseTUnitTests.LuaCallSupportsMultiReturnAndNilPadding
return function(value) return value end
