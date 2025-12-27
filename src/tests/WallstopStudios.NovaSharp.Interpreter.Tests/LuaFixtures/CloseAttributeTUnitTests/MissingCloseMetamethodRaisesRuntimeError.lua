-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/CloseAttributeTUnitTests.cs:133
-- @test: CloseAttributeTUnitTests.MissingCloseMetamethodRaisesRuntimeError
-- @compat-notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local ok, err = pcall(function()
                    local _ <close> = {}
                end)
                return ok, err
