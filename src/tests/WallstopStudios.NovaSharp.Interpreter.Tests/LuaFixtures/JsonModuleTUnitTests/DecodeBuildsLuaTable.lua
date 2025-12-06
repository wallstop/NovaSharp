-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/JsonModuleTUnitTests.cs:45
-- @test: JsonModuleTUnitTests.DecodeBuildsLuaTable
-- @compat-notes: Lua 5.3+: bitwise operators
local m = require('json'); json = { encode = m.serialize, decode = m.parse };
