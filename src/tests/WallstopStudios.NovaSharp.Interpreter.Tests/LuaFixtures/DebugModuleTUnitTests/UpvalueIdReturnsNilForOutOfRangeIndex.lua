-- @lua-versions: 5.2, 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1229
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsNilForOutOfRangeIndex
-- @compat-notes: Lua 5.2+: _ENV variable
local function f()
                    -- Has _ENV as upvalue but nothing else
                end
                return debug.upvalueid(f, 999)
