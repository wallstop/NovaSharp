-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMethodsTUnitTests.cs:1254
-- @test: SomeOtherClassWithDualInterfaces.InteropStaticInstanceAccessRaisesError
-- @compat-notes: Uses injected variable: myobj
t = { 'asd', 'qwe', 'zxc', ['x'] = 'X', ['y'] = 'Y' };
				x = mystatic.ConcatI(1, 'ciao', myobj, true, t, t, 'eheh', t, myobj);
				return x;
