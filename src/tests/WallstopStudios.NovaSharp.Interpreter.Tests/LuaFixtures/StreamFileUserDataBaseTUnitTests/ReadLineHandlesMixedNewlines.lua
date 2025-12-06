-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:811
-- @test: StreamFileUserDataBaseTUnitTests.ReadLineHandlesMixedNewlines
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                local a = f:read('*l')
                local b = f:read('*l')
                local c = f:read('*l')
                local d = f:read('*l')
                return a, b, c, d
