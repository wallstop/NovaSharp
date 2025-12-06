-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:788
-- @test: StreamFileUserDataBaseTUnitTests.ReadNumberStopsWhenLeadingSignCannotBeConsumed
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                local number = f:read('*n')
                local rest = f:read('*a')
                return number, rest
