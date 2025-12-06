-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:909
-- @test: StreamFileUserDataBaseTUnitTests.ReadNumberReturnsNilWithoutConsumingNonNumericData
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                local first = f:read('*n')
                local remainder = f:read(3)
                return first, remainder
