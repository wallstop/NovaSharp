-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:29
-- @test: DebugModuleTUnitTests.GetInfoReturnsFunctionReferenceForLuaFunctions
-- @compat-notes: Test targets Lua 5.1
local function sample() end
                local info = debug.getinfo(sample)
                return info.func == sample
