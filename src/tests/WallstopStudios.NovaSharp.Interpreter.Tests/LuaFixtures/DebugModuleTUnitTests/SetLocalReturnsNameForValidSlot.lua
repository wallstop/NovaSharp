-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:104
-- @test: DebugModuleTUnitTests.SetLocalReturnsNameForValidSlot
-- @compat-notes: Lua 5.3+: bitwise operators
local assigned = debug.setlocal(0, 1, 0)
                local missing = debug.setlocal(0, 42, 0)
                return assigned, missing
