-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LoadModuleTUnitTests.cs:219
-- @test: LoadModuleTUnitTests.LoadFileHonorsExplicitEnvironmentParameter
-- @compat-notes: Lua 5.3+: bitwise operators
local env = { value = 'from-env' }
                local fn = loadfile('module.lua', 't', env)
                return fn()
