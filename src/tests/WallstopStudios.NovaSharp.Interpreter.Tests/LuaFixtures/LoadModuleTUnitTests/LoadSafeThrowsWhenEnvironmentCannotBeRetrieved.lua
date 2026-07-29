-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:474
-- @test: LoadModuleTUnitTests.LoadSafeThrowsWhenEnvironmentCannotBeRetrieved
-- Compatibility notes: Test targets Lua 5.1; Lua 5.2+: _ENV variable
local original_env = _ENV
                local ls = loadsafe
                local pc = pcall
                _ENV = nil
                -- In Lua 5.1, even though we only use locals, _ENV is still captured as an upvalue.
                -- So when loadsafe looks for _ENV, it finds nil and fails.
                local ok, err = pc(function() return ls('return 1') end)
                _ENV = original_env
                return ok, err
