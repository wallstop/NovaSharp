-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:492
-- @test: IoModuleTUnitTests.StdStreamsAreAccessibleViaProperties
-- Test targets Lua 5.1
return io.stdin ~= nil, io.stdout ~= nil, io.stderr ~= nil, io.unknown == nil
