-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.TracebackDataDrivenLevels
-- @compat-notes: Tests debug.traceback with level 0

-- Test: debug.traceback with level 0 includes traceback itself
local function level3()
    return debug.traceback('marker', 0)
end
local function level2()
    return level3()
end
local function level1()
    return level2()
end
local result = level1()
assert(type(result) == 'string', "traceback should return a string")
assert(result:find('marker'), "traceback should include the message 'marker'")
assert(result:find('traceback'), "traceback should include 'traceback' header")
return true