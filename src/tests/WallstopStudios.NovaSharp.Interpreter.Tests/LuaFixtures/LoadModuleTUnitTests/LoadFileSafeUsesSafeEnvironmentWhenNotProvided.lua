-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:269
-- @test: LoadModuleTUnitTests.LoadFileSafeUsesSafeEnvironmentWhenNotProvided
-- @compat-notes: Test targets Lua 5.1
local fn = loadfilesafe('safe.lua'); return fn()
