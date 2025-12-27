-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:213
-- @test: IoModuleTUnitTests.TmpFileCreatesWritableFile
-- @compat-notes: Test targets Lua 5.1
local f = io.tmpfile()
                f:write('temp-data')
                return io.type(f)
