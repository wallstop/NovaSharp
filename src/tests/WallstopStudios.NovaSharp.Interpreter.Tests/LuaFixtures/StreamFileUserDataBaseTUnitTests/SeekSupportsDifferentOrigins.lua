-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:219
-- @test: StreamFileUserDataBaseTUnitTests.SeekSupportsDifferentOrigins
-- @compat-notes: Uses injected variable: file
local f = file
                local setPos = f:seek('set', 2)
                local char = f:read(1)
                local cur = f:seek()
                local fromEnd = f:seek('end', -1)
                return setPos, char, cur, fromEnd
