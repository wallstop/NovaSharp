-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2421
-- @test: DebugModuleTUnitTests.UpvalueIdDataDrivenSharedUpvalues
-- @compat-notes: Lua 5.2+: debug.upvalueid (5.2+)
local shared = 42
                local function f1() return shared end
                local function f2() return shared end
                local id1 = debug.upvalueid(f1, 1)
                local id2 = debug.upvalueid(f2, 1)
                return id1 == id2
