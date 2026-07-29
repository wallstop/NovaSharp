-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ModuleRegisterTUnitTests.cs:114
-- @test: ModuleRegisterTUnitTests.RegisterModuleTypeAcceptsArgumentViewCallbacks
return argument_view_probe.count(1, 2, 3)
