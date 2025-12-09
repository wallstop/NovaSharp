-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:193
-- @test: LoadModuleTUnitTests.LoadSafeThrowsWhenEnvironmentCannotBeRetrieved
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.2+: _ENV variable
local original_env = _ENV
                local ls = loadsafe
                local pc = pcall
                _ENV = nil
                local ok, err = pc(function() return ls('return 1') end)
                _ENV = original_env
                return ok, err
