-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:834
-- @test: IoModuleTUnitTests.OpenRejectsEncodingWhenBinaryModeSpecified
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, res1, res2 = pcall(function()
                    return io.open('{escapedPath}', 'wb', 'utf-8')
                end)
                return ok, res1, res2
