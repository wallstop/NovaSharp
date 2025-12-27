-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:1199
-- @test: IoModuleTUnitTests.TmpFileCreatesWritableStream
-- @compat-notes: Test targets Lua 5.1
local f = io.tmpfile()
                f:write('temp data')
                f:seek('set')
                local t_open = io.type(f)
                f:close()
                local t_closed = io.type(f)
                return t_open, t_closed
