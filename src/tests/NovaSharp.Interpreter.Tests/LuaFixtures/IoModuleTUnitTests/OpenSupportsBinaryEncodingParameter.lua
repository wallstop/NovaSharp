-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:464
-- @test: IoModuleTUnitTests.OpenSupportsBinaryEncodingParameter
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{path}', 'rb', 'binary'))
                local data = f:read('*a')
                f:close()
                return data
