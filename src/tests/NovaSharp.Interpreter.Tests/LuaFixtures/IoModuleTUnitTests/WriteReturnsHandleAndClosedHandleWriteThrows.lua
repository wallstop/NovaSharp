-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:775
-- @test: IoModuleTUnitTests.WriteReturnsHandleAndClosedHandleWriteThrows
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'w'))
                    local returned = f:write('payload')
                    f:close()
                    local ok, err = pcall(function() f:write('more') end)
                    return returned == f, ok, err
