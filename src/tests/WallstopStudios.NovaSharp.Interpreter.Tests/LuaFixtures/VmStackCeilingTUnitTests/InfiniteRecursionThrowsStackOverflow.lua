-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/VmStackCeilingTUnitTests.cs:64
-- @test: VmStackCeilingTUnitTests.InfiniteRecursionThrowsStackOverflow
local function f() return 1 + f() end return f()
