-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathVersionCompatibilityTUnitTests.cs:518
-- @test: MathVersionCompatibilityTUnitTests.DiagnosticDumpMathTableContents
local keys = {}
                for k, v in pairs(math) do
                    keys[#keys + 1] = k .. '=' .. type(v)
                end
                table.sort(keys)
                return table.concat(keys, ', ')
