-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:94
-- @test: CoroutineModuleTUnitTests.RegisteredCallbacksAndWrappedFunctionsUseArgumentViews
-- Compatibility notes: Test targets Lua 5.3+
wrapped = coroutine.wrap(function(value)
    coroutine.yield(value)
    return value + 1
end)

local first = wrapped(41)
local second = wrapped(41)
return first, second
