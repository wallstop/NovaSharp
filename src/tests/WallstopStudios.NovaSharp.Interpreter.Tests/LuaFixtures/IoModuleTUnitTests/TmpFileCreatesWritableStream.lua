-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:872
-- @test: IoModuleTUnitTests.TmpFileCreatesWritableStream
-- @compat-notes: Lua 5.3+: bitwise operators
local f = io.tmpfile()
                f:write('temp data')
                f:seek('set')
                local t_open = io.type(f)
                f:close()
                local t_closed = io.type(f)
                return t_open, t_closed
