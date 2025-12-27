-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:470
-- @test: StreamFileUserDataBaseTUnitTests.ReadParsesNumbersWithLeadingDecimal
-- @compat-notes: Uses injected variable: file
local f = file
                local num = f:read('*n')
                local remainder = f:read('*a')
                return num, remainder
