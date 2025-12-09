-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LuaBaseTUnitTests.cs:207
-- @test: LuaBaseTUnitTests.LuaGetTableReturnsRawValuesAndThrowsOnNonTables
-- @compat-notes: Lua 5.3+: bitwise operators
return { answer = 42 }
