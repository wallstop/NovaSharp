-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:648
-- @test: IoModuleTUnitTests.InputReturnsCurrentFileWhenNoArguments
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'r'))
                    io.input(f)
                    local current = io.input()
                    return io.Type(current), io.Type(f)
