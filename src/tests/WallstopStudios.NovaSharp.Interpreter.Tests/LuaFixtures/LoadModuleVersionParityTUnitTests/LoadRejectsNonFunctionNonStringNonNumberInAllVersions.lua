-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:283
-- @test: LoadModuleVersionParityTUnitTests.LoadRejectsNonFunctionNonStringNonNumberInAllVersions
load({})
