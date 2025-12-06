-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:37
-- @test: StreamFileUserDataBaseTUnitTests.WriteAppendsTextAndReturnsSelf
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                f:seek('end')
                return f:write('A', 'B')
