-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:171
-- @test: IoModuleTUnitTests.TmpFileCreatesWritableFile
-- @compat-notes: Lua 5.3+: bitwise operators
local f = io.tmpfile()
                f:write('temp-data')
                return io.type(f)
