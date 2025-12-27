-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1407
-- @test: SimpleTUnitTests.VarArgsSum2
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: table.pack (5.2+)
function x(m, ...)
						local t = table.pack(...);
						local sum = 0;

						for i = 1, #t do
							sum = sum + t[i];
						end
	
						return sum * m;
					end

					return x(5,1,2,3,4);
