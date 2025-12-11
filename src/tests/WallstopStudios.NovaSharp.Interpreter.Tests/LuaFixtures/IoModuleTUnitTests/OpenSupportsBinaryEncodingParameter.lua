-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:484
-- @test: IoModuleTUnitTests.OpenSupportsBinaryEncodingParameter
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local f = assert(io.open('{path}', 'rb', 'binary'))
                local data = f:read('*a')
                f:close()
                return data
