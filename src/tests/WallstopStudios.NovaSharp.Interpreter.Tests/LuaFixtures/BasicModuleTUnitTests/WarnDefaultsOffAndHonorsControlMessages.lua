-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:359
-- @test: BasicModuleTUnitTests.WarnDefaultsOffAndHonorsControlMessages
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: warn function
warn('disabled')
warn('@unknown')
warn('@on')
warn('enabled-', 9)
warn('@unknown')
warn('@off', '-is-data')
warn('@off')
warn('disabled-again')
