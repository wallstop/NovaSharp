-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:36
-- @test: ErrorHandlingModuleTUnitTests.PcallCapturesScriptErrors
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function() error('boom') end)
                return ok, err
