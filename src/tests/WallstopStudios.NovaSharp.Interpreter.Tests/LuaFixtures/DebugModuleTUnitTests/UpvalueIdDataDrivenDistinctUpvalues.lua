-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2466
-- @test: DebugModuleTUnitTests.UpvalueIdDataDrivenDistinctUpvalues
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: debug.upvalueid (5.2+)
local x = 1
                local y = 2
                local function f() return x + y end
                local id1 = debug.upvalueid(f, 1)
                local id2 = debug.upvalueid(f, 2)
                return id1 ~= id2
