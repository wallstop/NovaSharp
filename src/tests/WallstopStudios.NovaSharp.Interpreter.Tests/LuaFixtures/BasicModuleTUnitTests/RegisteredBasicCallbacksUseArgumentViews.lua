-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:1632
-- @test: BasicModuleTUnitTests.RegisteredBasicCallbacksUseArgumentViews
-- The common callback path runs for every compatibility version.
local count = select('#', 'a', 'b', 'c')
assert(count == 3, 'select count mismatch')
assert(select(2, 'a', 'b', 'c') == 'b')
print('value:', 42)
return tostring(42)
