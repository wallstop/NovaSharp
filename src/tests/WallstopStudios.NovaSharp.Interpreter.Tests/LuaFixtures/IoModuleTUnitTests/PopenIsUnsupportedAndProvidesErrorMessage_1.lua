-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:1317
-- @test: IoModuleTUnitTests.PopenIsUnsupportedAndProvidesErrorMessage
-- @compat-notes: Test targets Lua 5.1
local ok, err = pcall(function() return io.popen('echo hello') end)
                return ok, err
