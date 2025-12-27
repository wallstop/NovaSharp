-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:909
-- @test: StreamFileUserDataBaseTUnitTests.ReadNumberReturnsNilWithoutConsumingNonNumericData
-- @compat-notes: Uses injected variable: file
local f = file
                local first = f:read('*n')
                local remainder = f:read(3)
                return first, remainder
