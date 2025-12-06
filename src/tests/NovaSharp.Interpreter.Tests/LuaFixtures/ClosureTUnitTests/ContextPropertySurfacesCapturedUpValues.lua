-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ClosureTUnitTests.cs:165
-- @test: ClosureTUnitTests.ContextPropertySurfacesCapturedUpValues
-- @compat-notes: Lua 5.3+: bitwise operators
local captured = 99
                return function()
                    return captured
                end
