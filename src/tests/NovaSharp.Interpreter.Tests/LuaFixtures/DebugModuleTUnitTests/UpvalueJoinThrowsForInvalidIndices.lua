-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:436
-- @test: DebugModuleTUnitTests.UpvalueJoinThrowsForInvalidIndices
-- @compat-notes: Lua 5.3+: bitwise operators
local function f() end
                local ok, err = pcall(function() debug.upvaluejoin(f, 999, f, 1) end)
                return ok, err
