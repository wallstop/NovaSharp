-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:284
-- @test: Lua55SpecTUnitTests.CoroutineStatusReportsCorrectState
-- @compat-notes: Lua 5.3+: bitwise operators
local co = coroutine.create(function() end)
                local before = coroutine.status(co)
                coroutine.resume(co)
                local after = coroutine.status(co)
                return before, after
