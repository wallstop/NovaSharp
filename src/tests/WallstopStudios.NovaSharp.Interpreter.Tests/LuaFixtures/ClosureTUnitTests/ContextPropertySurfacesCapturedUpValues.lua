-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:184
-- @test: ClosureTUnitTests.ContextPropertySurfacesCapturedUpValues
local captured = 99
                return function()
                    return captured
                end
