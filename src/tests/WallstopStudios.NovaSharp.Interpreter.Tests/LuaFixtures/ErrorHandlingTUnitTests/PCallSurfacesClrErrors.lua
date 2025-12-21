-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ErrorHandlingTUnitTests.cs:35
-- @test: ErrorHandlingTUnitTests.PCallSurfacesClrErrors
-- @compat-notes: Uses injected variable: r
r, msg = pcall(assert, false, 'caught')
                return r, msg;
