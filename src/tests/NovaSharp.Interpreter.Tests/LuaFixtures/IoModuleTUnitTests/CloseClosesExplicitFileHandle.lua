-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:588
-- @test: IoModuleTUnitTests.CloseClosesExplicitFileHandle
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'w'))
                    local result = io.close(f)
                    return result, io.Type(f)
