-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:533
-- @test: IoModuleTUnitTests.SetDefaultFileOverridesStdOutStream
-- Compatibility notes: Test method 'SetDefaultFileOverridesStdOutStream' tests NovaSharp-specific behavior (SetDefaultFileOverridesStdOutStream)
io.write('buffered'); io.flush()
