-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:95
-- @test: DebugModuleTUnitTests.GetInfoSurfacesArgumentErrors
-- @compat-notes: Test targets Lua 5.1
local ok, err = pcall(function() debug.getinfo('bad') end)
                return ok, err
