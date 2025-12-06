-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:20
-- @test: DebugModuleTUnitTests.GetInfoReturnsFunctionReferenceForLuaFunctions
-- @compat-notes: Lua 5.3+: bitwise operators
local function sample() end
                local info = debug.getinfo(sample)
                return info.func == sample
