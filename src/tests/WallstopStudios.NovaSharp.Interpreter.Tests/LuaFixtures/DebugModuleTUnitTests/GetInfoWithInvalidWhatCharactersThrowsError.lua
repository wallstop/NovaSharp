-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2946
-- @test: DebugModuleTUnitTests.GetInfoWithInvalidWhatCharactersThrowsError
-- @compat-notes: Test targets Lua 5.1
local function sample() end
                local info = debug.getinfo(sample, 'nXYZ')
                return info.name, info.source
