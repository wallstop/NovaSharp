-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:381
-- @test: LoadModuleTUnitTests.LoadFileHonorsExplicitEnvironmentParameter
-- @compat-notes: Test targets Lua 5.1
local env = { value = 'from-env' }
                local fn = loadfile('module.lua', 't', env)
                return fn()
