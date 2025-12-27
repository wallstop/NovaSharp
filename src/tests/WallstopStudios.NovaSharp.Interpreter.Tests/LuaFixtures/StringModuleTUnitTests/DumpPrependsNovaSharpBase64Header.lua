-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:710
-- @test: StringModuleTUnitTests.DumpPrependsNovaSharpBase64Header
local function increment(x) return x + 1 end
                return string.dump(increment)
