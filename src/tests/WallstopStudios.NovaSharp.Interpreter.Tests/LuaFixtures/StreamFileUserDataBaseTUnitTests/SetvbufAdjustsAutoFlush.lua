-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:336
-- @test: StreamFileUserDataBaseTUnitTests.SetvbufAdjustsAutoFlush
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                local first = f:setvbuf('line')
                local second = f:setvbuf('full')
                return first, second
