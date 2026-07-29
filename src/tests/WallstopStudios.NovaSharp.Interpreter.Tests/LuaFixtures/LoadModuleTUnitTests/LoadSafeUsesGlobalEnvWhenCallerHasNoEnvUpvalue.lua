-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:507
-- @test: LoadModuleTUnitTests.LoadSafeUsesGlobalEnvWhenCallerHasNoEnvUpvalue
-- Compatibility notes: NovaSharp: using statement (non-Lua); Test targets Lua 5.1
local ls = loadsafe
                -- This function only uses the local 'ls', so it has no _ENV upvalue in Lua 5.2+.
                -- loadsafe should fall back to using the script's global environment.
                local fn = ls('return 42')
                return fn()
