-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:222
-- @test: LoadModuleVersionParityTUnitTests.LoadsafeRejectsStringsInLua51
-- @compat-notes: Test targets Lua 5.1
loadsafe('return 1')
