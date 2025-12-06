-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:151
-- @test: IoModuleTUnitTests.TmpFileCreatesWritableFile
-- @compat-notes: Lua 5.3+: bitwise operators
local f = io.tmpfile()
                f:write('temp-data')
                return io.Type(f)
