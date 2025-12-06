-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:859
-- @test: StreamFileUserDataBaseTUnitTests.ReadToEndAfterLineReadsReturnsRemainingContent
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                local first = f:read('*l')
                local remainder = f:read('*a')
                return first, remainder
