-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\JsonModuleTUnitTests.cs:50
-- @test: JsonModuleTUnitTests.DecodeBuildsLuaTable
-- Test class 'JsonModuleTUnitTests' uses NovaSharp-specific JsonModule functionality
local m = require('json'); json = { encode = m.serialize, decode = m.parse };
