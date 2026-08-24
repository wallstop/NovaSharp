-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:1648
-- @test: BasicModuleTUnitTests.RegisteredBasicCallbacksUseArgumentViews
-- Compatibility notes: Test targets Lua 5.1
local function reader() return payload end
setfenv(reader, { payload = 7 })
return reader()
