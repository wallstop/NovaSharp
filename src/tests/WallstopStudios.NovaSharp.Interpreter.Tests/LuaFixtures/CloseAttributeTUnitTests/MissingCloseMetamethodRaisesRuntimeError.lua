-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/CloseAttributeTUnitTests.cs:117
-- @test: CloseAttributeTUnitTests.MissingCloseMetamethodRaisesRuntimeError
-- @compat-notes: Lua 5.4: close attribute; Lua 5.3+: bitwise operators
local ok, err = pcall(function()
                    local _ <close> = {}
                end)
                return ok, err
