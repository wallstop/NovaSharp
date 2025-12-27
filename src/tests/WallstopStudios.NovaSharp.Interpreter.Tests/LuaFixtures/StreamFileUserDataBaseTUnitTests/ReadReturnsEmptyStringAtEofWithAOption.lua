-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1009
-- @test: StreamFileUserDataBaseTUnitTests.ReadReturnsEmptyStringAtEofWithAOption
-- @compat-notes: Uses injected variable: file
local f = file
                local first = f:read('*a')
                local second = f:read('*a')
                return first, second
