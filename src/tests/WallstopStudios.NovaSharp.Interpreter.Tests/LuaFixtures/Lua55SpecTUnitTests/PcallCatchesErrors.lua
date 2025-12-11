-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\Lua55SpecTUnitTests.cs:374
-- @test: Lua55SpecTUnitTests.PcallCatchesErrors
-- @compat-notes: Lua 5.3+: bitwise operators
local function bad() error('test error') end
                local ok, msg = pcall(bad)
                return ok, type(msg)
