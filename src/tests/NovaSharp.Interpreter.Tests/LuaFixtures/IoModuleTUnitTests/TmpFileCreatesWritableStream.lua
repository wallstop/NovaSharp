-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:852
-- @test: IoModuleTUnitTests.TmpFileCreatesWritableStream
-- @compat-notes: Lua 5.3+: bitwise operators
local f = io.tmpfile()
                f:write('temp data')
                f:seek('set')
                local t_open = io.Type(f)
                f:close()
                local t_closed = io.Type(f)
                return t_open, t_closed
