-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ErrorHandlingModuleTUnitTests.cs:35
-- @test: ErrorHandlingModuleTUnitTests.PcallCapturesScriptErrors
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function() error('boom') end)
                return ok, err
