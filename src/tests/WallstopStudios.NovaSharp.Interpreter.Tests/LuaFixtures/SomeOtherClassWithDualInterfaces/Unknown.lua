-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\UserDataMethodsTUnitTests.cs:532
-- @test: SomeOtherClassWithDualInterfaces.Unknown
-- Uses injected variable: myobj
return myobj.format('{0}.{1}@{2}:{3}', 1, 2, 'ciao', true);
