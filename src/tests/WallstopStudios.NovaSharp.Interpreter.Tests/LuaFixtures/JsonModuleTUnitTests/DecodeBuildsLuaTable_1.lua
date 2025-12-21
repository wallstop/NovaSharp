-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/JsonModuleTUnitTests.cs:59
-- @test: JsonModuleTUnitTests.DecodeBuildsLuaTable
-- @compat-notes: Test class 'JsonModuleTUnitTests' uses NovaSharp-specific JsonModule functionality
local data = json.decode('{"name":"nova","values":[10,20]}')
                return data.name, data.values[1], data.values[2]
