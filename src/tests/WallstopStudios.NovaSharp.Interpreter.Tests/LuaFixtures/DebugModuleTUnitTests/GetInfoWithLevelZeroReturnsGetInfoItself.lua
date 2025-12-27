-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:3008
-- @test: DebugModuleTUnitTests.GetInfoWithLevelZeroReturnsGetInfoItself
-- @compat-notes: Test targets Lua 5.1
local info = debug.getinfo(0, 'nS')
                return info.what, info.source
