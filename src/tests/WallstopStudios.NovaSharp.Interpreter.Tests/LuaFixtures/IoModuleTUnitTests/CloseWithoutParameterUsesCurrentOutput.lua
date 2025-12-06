-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:607
-- @test: IoModuleTUnitTests.CloseWithoutParameterUsesCurrentOutput
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'w'))
                    io.output(f)
                    local closed = io.close()
                    return closed, io.Type(f)
