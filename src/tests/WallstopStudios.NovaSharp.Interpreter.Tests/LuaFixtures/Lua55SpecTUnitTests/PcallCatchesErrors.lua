-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:405
-- @test: Lua55SpecTUnitTests.PcallCatchesErrors
-- @compat-notes: Test targets Lua 5.5+
local function bad() error('test error') end
                local ok, msg = pcall(bad)
                return ok, type(msg)
