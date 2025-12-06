-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/JsonModuleTUnitTests.cs:48
-- @test: JsonModuleTUnitTests.DecodeBuildsLuaTable
-- @compat-notes: Lua 5.3+: bitwise operators
local data = json.decode('{"name":"nova","values":[10,20]}')
                return data.name, data.values[1], data.values[2]
