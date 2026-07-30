-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/VmStackCeilingTUnitTests.cs:76
-- @test: VmStackCeilingTUnitTests.StackOverflowIsCatchableViaPcall
local function f() return 1 + f() end
                local ok, err = pcall(f)
                return ok, err
