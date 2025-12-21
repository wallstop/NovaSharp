-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:39
-- @test: ErrorHandlingModuleTUnitTests.PcallCapturesScriptErrors
local ok, err = pcall(function() error('boom') end)
                return ok, err
