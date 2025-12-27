-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:336
-- @test: StreamFileUserDataBaseTUnitTests.SetvbufAdjustsAutoFlush
-- @compat-notes: Uses injected variable: file
local f = file
                local first = f:setvbuf('line')
                local second = f:setvbuf('full')
                return first, second
