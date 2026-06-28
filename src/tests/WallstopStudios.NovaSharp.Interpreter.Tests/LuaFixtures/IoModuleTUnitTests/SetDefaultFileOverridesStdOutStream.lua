-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:533
-- @test: IoModuleTUnitTests.SetDefaultFileOverridesStdOutStream
-- Requires C#-configured stdout stream
io.write('buffered'); io.flush()
