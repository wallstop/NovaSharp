-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:1317
-- @test: IoModuleTUnitTests.PopenIsUnsupportedAndProvidesErrorMessage
-- @compat-notes: Test method 'PopenIsUnsupportedAndProvidesErrorMessage' tests NovaSharp-specific behavior (IsUnsupported)
local ok, err = pcall(function() return io.popen('echo hello') end)
                return ok, err
