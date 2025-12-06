-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:731
-- @test: IoModuleTUnitTests.TypeReportsClosedFileState
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'r'))
                    local before = io.type(f)
                    f:close()
                    local after = io.type(f)
                    return before, after
