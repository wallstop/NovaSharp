-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1307
-- @test: DebugModuleTUnitTests.GetLocalFromFunctionReturnsUpValuePlaceholder
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.getlocal with function var (5.2+)
local x = 10
                local function closure()
                    return x
                end
                return debug.getlocal(closure, 1)
