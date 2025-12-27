-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:307
-- @test: Lua55SpecTUnitTests.CoroutineStatusReportsCorrectState
-- @compat-notes: Test targets Lua 5.5+
local co = coroutine.create(function() end)
                local before = coroutine.status(co)
                coroutine.resume(co)
                local after = coroutine.status(co)
                return before, after
