-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:1724
-- @test: BasicModuleTUnitTests.BasicModuleViewMethodsAvoidLegacyArrayMaterialization
-- Compatibility notes: Test targets Lua 5.4+
return setmetatable({}, { __tostring = function() return 'value' end })
