-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:515
-- @test: IoModuleTUnitTests.SetDefaultFileOverridesStdInStream
-- @compat-notes: Test targets Lua 5.1
return io.read('*l')
