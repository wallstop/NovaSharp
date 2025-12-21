-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:1176
-- @test: IoModuleTUnitTests.OpenRejectsEncodingWhenBinaryModeSpecified
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local ok, res1, res2 = pcall(function()
                    return io.open('{escapedPath}', 'wb', 'utf-8')
                end)
                return ok, res1, res2
