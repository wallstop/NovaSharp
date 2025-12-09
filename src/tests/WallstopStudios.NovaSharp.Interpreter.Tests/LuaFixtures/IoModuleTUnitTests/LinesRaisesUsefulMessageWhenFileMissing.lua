-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:705
-- @test: IoModuleTUnitTests.LinesRaisesUsefulMessageWhenFileMissing
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: file
local ok, err = pcall(function() return io.lines('missing-file.txt') end)
                return ok, err
