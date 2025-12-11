-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:399
-- @test: IoModuleTUnitTests.SetDefaultFileOverridesStdInStream
return io.read('*l')
