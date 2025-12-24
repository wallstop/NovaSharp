-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathVersionCompatibilityTUnitTests.cs:543
-- @test: MathVersionCompatibilityTUnitTests.DiagnosticDumpMathTableContents
-- @compat-notes: Test targets Lua 5.1
local keys = {}
                for k, v in pairs(math) do
                    keys[#keys + 1] = k .. '=' .. type(v)
                end
                table.sort(keys)
                return table.concat(keys, ', ')
