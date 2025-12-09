-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1054
-- @test: StreamFileUserDataBaseTUnitTests.LinesEnumeratorTerminatesAtNil
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: file
local f = file
                local output = {}
                for line in f:lines() do
                    table.insert(output, line)
                end
                return table.concat(output, ',')
