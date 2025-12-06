-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1342
-- @test: StreamFileUserDataBaseTUnitTests.SeekWithCurWhenceUsesCurrentPosition
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                f:seek('set', 3)
                local fromCur = f:seek('cur', 2)
                local char = f:read(1)
                return fromCur, char
