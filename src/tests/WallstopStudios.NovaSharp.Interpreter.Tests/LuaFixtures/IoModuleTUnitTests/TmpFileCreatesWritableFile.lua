-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:213
-- @test: IoModuleTUnitTests.TmpFileCreatesWritableFile
-- Test targets Lua 5.1
local f = io.tmpfile()
                f:write('temp-data')
                f:seek('set', 0)
                return io.type(f), f:read('*a')
