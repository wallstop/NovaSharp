-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:344
-- @test: DebugModuleTUnitTests.GetMetatableReturnsNilForTypesWithoutMetatable
return debug.getmetatable(function() end)
