-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:326
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsNilForInvalidIndexLua54Plus
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.upvalueid (5.2+)
local function f() end
                return debug.upvalueid(f, 999)
