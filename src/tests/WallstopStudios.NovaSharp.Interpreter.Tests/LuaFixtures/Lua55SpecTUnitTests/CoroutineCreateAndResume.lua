-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\Lua55SpecTUnitTests.cs:268
-- @test: Lua55SpecTUnitTests.CoroutineCreateAndResume
-- @compat-notes: Lua 5.3+: bitwise operators
local co = coroutine.create(function(x) return x * 2 end)
                local ok, val = coroutine.resume(co, 21)
                return ok, val
