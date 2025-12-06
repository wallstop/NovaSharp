-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1009
-- @test: StreamFileUserDataBaseTUnitTests.ReadReturnsEmptyStringAtEofWithAOption
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                local first = f:read('*a')
                local second = f:read('*a')
                return first, second
