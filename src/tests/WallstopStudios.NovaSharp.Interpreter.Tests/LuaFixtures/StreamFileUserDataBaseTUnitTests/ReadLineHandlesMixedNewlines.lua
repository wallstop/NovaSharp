-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:811
-- @test: StreamFileUserDataBaseTUnitTests.ReadLineHandlesMixedNewlines
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: file
local f = file
                local a = f:read('*l')
                local b = f:read('*l')
                local c = f:read('*l')
                local d = f:read('*l')
                return a, b, c, d
