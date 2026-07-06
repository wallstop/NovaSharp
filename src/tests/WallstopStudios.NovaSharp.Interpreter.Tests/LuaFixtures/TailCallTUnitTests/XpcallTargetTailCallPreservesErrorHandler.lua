-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:453
-- @test: TailCallTUnitTests.XpcallTargetTailCallPreservesErrorHandler
local function target()
                    error('tail boom', 0)
                end

                return xpcall(function()
                    return target()
                end, function(message)
                    return 'handled:' .. message
                end)
