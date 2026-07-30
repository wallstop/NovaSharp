-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/VmStackCeilingTUnitTests.cs:259
-- @test: VmStackCeilingTUnitTests.CoroutineInheritsBakedCeilingAfterOptionMutation
local co = coroutine.create(function()
                    local function f(n) return 1 + f(n + 1) end
                    return f(0)
                end)
                local ok, err = coroutine.resume(co)
                return ok, tostring(err)
