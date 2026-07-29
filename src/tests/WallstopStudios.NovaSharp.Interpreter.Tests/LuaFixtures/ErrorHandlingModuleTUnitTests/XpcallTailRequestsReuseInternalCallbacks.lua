-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:556
-- @test: ErrorHandlingModuleTUnitTests.XpcallTailRequestsReuseInternalCallbacks
return function() return 1 end
