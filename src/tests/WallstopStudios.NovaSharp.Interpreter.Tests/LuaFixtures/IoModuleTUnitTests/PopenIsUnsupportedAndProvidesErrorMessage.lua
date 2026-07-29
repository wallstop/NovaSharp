-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:1314
-- @test: IoModuleTUnitTests.PopenIsUnsupportedAndProvidesErrorMessage
-- Test method 'PopenIsUnsupportedAndProvidesErrorMessage' tests NovaSharp-specific behavior (IsUnsupported)
return type(io.popen)
