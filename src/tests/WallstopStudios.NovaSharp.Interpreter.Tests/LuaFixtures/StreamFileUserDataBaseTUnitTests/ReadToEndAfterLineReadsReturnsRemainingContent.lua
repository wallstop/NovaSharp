-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:859
-- @test: StreamFileUserDataBaseTUnitTests.ReadToEndAfterLineReadsReturnsRemainingContent
-- @compat-notes: Uses injected variable: file
local f = file
                local first = f:read('*l')
                local remainder = f:read('*a')
                return first, remainder
