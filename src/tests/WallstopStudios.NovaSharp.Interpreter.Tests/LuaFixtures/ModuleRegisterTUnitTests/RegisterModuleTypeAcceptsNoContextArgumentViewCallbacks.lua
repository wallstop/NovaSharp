-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ModuleRegisterTUnitTests.cs:130
-- @test: ModuleRegisterTUnitTests.RegisterModuleTypeAcceptsNoContextArgumentViewCallbacks
-- Compatibility notes: Test targets Lua 5.1
return argument_view_no_context_probe.count(1, 2, 3, 4)
