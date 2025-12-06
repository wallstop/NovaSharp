-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:219
-- @test: StreamFileUserDataBaseTUnitTests.SeekSupportsDifferentOrigins
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                local setPos = f:seek('set', 2)
                local char = f:read(1)
                local cur = f:seek()
                local fromEnd = f:seek('end', -1)
                return setPos, char, cur, fromEnd
