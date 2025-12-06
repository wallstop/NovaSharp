-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/LuaBaseTUnitTests.cs:74
-- @test: LuaBaseTUnitTests.LuaTypeHandlesBooleansStringsFunctionsAndTables
-- @compat-notes: Lua 5.3+: bitwise operators
return { key = 'value' }
