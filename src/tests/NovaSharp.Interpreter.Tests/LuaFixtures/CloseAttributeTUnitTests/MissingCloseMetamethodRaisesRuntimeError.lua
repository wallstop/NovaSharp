-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/CloseAttributeTUnitTests.cs:117
-- @test: CloseAttributeTUnitTests.MissingCloseMetamethodRaisesRuntimeError
-- @compat-notes: Lua 5.4: close attribute; Lua 5.3+: bitwise operators
local ok, err = pcall(function()
                    local _ <close> = {}
                end)
                return ok, err
