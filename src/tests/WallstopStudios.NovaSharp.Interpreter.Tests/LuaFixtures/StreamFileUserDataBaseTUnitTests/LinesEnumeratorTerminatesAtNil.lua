-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1054
-- @test: StreamFileUserDataBaseTUnitTests.LinesEnumeratorTerminatesAtNil
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                local output = {}
                for line in f:lines() do
                    table.insert(output, line)
                end
                return table.concat(output, ',')
