-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:290
-- @test: Lua55SpecTUnitTests.CoroutineCreateAndResume
local co = coroutine.create(function(x) return x * 2 end)
                local ok, val = coroutine.resume(co, 21)
                return ok, val
