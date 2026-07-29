-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:1305
-- @test: BasicModuleTUnitTests.PrintWorksWithClrFunctionTostringInLua51To53
-- Compatibility notes: Test targets Lua 5.1
return setmetatable({}, { __tostring = function() return 'value' end })
