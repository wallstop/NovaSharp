-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:157
-- @test: OsSystemModuleTUnitTests.RemoveDeletesExistingFile
-- @compat-notes: Uses injected variable: file
return os.remove('file.txt')
