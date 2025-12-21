-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:239
-- @test: LoadModuleVersionParityTUnitTests.LoadsafeAcceptsStringsInLua52Plus
-- @compat-notes: Test targets Lua 5.1
local f = loadsafe('return 99')
                return f()
