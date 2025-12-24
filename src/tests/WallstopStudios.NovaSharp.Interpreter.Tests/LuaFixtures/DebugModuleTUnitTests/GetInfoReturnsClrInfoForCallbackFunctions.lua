-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:736
-- @test: DebugModuleTUnitTests.GetInfoReturnsClrInfoForCallbackFunctions
-- @compat-notes: Test targets Lua 5.1
local info = debug.getinfo(print)
                return info.what, info.source
