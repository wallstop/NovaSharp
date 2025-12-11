-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleVirtualizationTUnitTests.cs:132
-- @test: IoModuleVirtualizationTUnitTests.IoWriteTargetsVirtualStdOut
-- @compat-notes: Test class 'IoModuleVirtualizationTUnitTests' uses NovaSharp-specific IoModuleVirtualization functionality
io.write('first'); io.write('second'); io.flush();
