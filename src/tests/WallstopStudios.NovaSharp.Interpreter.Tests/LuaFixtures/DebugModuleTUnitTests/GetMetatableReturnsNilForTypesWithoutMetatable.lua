-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:330
-- @test: DebugModuleTUnitTests.GetMetatableReturnsNilForTypesWithoutMetatable
return debug.getmetatable(function() end)
