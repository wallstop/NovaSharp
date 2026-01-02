-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetInfoWithInvalidWhatCharactersThrowsError
-- @compat-notes: Invalid what characters throw an error (not silently ignored)

-- Test: Invalid characters in 'what' string throw an error
local function sample() end
local info = debug.getinfo(sample, 'nXYZ')

-- This line should not be reached - the above should throw
return false