-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2973
-- @test: DebugModuleTUnitTests.GetInfoWithOnlyNFlagReturnsNameInfo
-- @compat-notes: Test targets Lua 5.1
local function sample() end
                local info = debug.getinfo(sample, 'n')
                return info.namewhat, info.source
