-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:925
-- @test: IoModuleTUnitTests.OutputCanBeRedirectedToCustomFile
-- @compat-notes: Lua 5.3+: bitwise operators
local original = io.output()
                    local temp = assert(io.open('{escapedPath}', 'w'))
                    io.output(temp)
                    io.write('hello world')
                    io.flush()
                    io.output(original)
                    temp:close()
