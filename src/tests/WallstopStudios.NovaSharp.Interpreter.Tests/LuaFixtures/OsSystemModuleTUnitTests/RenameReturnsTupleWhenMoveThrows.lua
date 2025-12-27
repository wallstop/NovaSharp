-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:252
-- @test: OsSystemModuleTUnitTests.RenameReturnsTupleWhenMoveThrows
-- @compat-notes: Test class 'OsSystemModuleTUnitTests' uses NovaSharp-specific OsSystemModule functionality
return os.rename('source', 'dest')
