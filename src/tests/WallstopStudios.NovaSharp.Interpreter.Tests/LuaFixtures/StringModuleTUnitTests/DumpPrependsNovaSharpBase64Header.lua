-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:986
-- @test: StringModuleTUnitTests.DumpPrependsNovaSharpBase64Header
-- @compat-notes: Test targets Lua 5.1
local function increment(x) return x + 1 end
                return string.dump(increment)
