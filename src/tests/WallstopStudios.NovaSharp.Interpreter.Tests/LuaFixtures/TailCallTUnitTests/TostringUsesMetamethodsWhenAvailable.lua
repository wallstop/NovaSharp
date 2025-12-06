-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/TailCallTUnitTests.cs:147
-- @test: TailCallTUnitTests.TostringUsesMetamethodsWhenAvailable
-- @compat-notes: Lua 5.3+: bitwise operators
local target = {}
                local meta = {
                    __tostring = function()
                        return 'ciao'
                    end
                }

                setmetatable(target, meta)
                return tostring(target)
