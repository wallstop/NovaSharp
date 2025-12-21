-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:201
-- @test: LoadModuleVersionParityTUnitTests.LoadUsesEnvParameterInLua52Plus
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: load with string arg (5.2+)
local env = { x = 100 }
                local f = load('return x', 'chunk', 't', env)
                return f()
