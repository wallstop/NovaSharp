-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:1724
-- @test: BasicModuleTUnitTests.RegisteredBasicCallbacksUseArgumentViews
-- The warning-specific callback path runs for Lua 5.4+.
warn('@on'); warn('caution', 9); warn('@off')
