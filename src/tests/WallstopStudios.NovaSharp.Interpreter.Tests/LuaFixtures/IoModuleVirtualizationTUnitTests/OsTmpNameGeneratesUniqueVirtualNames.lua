-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:111
-- @test: IoModuleVirtualizationTUnitTests.OsTmpNameGeneratesUniqueVirtualNames
-- @compat-notes: Test class 'IoModuleVirtualizationTUnitTests' uses NovaSharp-specific IoModuleVirtualization functionality
return os.tmpname()
