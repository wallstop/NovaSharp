-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/JsonModuleTUnitTests.cs:45
-- @test: JsonModuleTUnitTests.DecodeBuildsLuaTable
-- @compat-notes: NovaSharp: NovaSharp json module
local m = require('json'); json = { encode = m.serialize, decode = m.parse };
