-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:817
-- @test: IoModuleTUnitTests.OpenReturnsErrorTupleForUnknownEncoding
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local file, message = io.open('{escapedPath}', 'w', 'does-not-exist')
                return file, message
