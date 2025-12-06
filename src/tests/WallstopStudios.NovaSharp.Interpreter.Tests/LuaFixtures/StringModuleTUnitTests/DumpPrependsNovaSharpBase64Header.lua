-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:341
-- @test: StringModuleTUnitTests.DumpPrependsNovaSharpBase64Header
local function increment(x) return x + 1 end
                return string.dump(increment)
