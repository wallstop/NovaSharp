-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:1025
-- @test: IoModuleTUnitTests.LinesMethodSupportsReadOptions
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'r'))
                io.input(f)
                local number = io.read('*n')
                local remainder = io.read('*a')
                f:close()
                return number, remainder
