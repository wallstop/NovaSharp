-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:37
-- @test: StreamFileUserDataBaseTUnitTests.WriteAppendsTextAndReturnsSelf
-- @compat-notes: Uses injected variable: file
local f = file
                f:seek('end')
                return f:write('A', 'B')
