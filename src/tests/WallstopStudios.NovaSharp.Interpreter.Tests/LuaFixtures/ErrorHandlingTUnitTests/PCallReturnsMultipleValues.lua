-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ErrorHandlingTUnitTests.cs:19
-- @test: ErrorHandlingTUnitTests.PCallReturnsMultipleValues
return pcall(function() return 1,2,3 end)
