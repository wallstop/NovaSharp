-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:685
-- @test: IoModuleTUnitTests.LinesRaisesUsefulMessageWhenFileMissing
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function() return io.lines('missing-file.txt') end)
                return ok, err
