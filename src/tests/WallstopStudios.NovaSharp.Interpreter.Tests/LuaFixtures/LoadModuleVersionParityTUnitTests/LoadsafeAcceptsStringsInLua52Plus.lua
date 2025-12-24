-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:239
-- @test: LoadModuleVersionParityTUnitTests.LoadsafeAcceptsStringsInLua52Plus
-- @compat-notes: Test targets Lua 5.1
local f = loadsafe('return 99')
return f()
