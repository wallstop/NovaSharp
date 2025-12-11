-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:968
-- @test: IoModuleTUnitTests.PopenIsUnsupportedAndProvidesErrorMessage
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function() return io.popen('echo hello') end)
                return ok, err
