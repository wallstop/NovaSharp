namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Selected acceptance tests extracted from the official Lua suite.
    /// </summary>
    public sealed class LuaTestSuiteExtractTUnitTests
    {
        [global::TUnit.Core.Test]
        public Task LuaSuiteCallsLocalFunctionRecursion()
        {
            return RunTestAsync(
                @"
                -- testing local-function recursion
                fact = false
                do
                  local res = 1
                  local function fact (n)
                    if n==0 then return res
                    else return n*fact(n-1)
                    end
                  end
                  xassert('fact(5) == 120', fact(5) == 120)
                end
                xassert('fact == false', fact == false)
                "
            );
        }

        [global::TUnit.Core.Test]
        public Task LuaSuiteCallsDeclarations()
        {
            return RunTestAsync(
                @"
                -- testing local-function recursion
                -- testing declarations
                a = {i = 10}
                self = 20
                function a:x (x) return x+self.i end
                function a.y (x) return x+self end

                xassert('a:x(1)+10 == a.y(1)', a:x(1)+10 == a.y(1))

                a.t = {i=-100}
                a['t'].x = function (self, a,b) return self.i+a+b end

                xassert('a.t:x(2,3) == -95', a.t:x(2,3) == -95)

                do
                  local a = {x=0}
                  function a:add (x) self.x, a.y = self.x+x, 20; return self end
                  xassert('a:add(10):add(20):add(30).x == 60 and a.y == 20', a:add(10):add(20):add(30).x == 60 and a.y == 20)
                end

                local a = {b={c={}}}

                function a.b.c.f1 (x) return x+1 end
                function a.b.c:f2 (x,y) self[x] = y end
                xassert('a.b.c.f1(4) == 5', a.b.c.f1(4) == 5)
                a.b.c:f2('k', 12); xassert('a.b.c.k == 12', a.b.c.k == 12)

                print('+')

                t = nil   -- 'declare' t
                function f(a,b,c) local d = 'a'; t={a,b,c,d} end

                f(      -- this line change must be valid
                  1,2)
                xassert('missingparam', t[1] == 1 and t[2] == 2 and t[3] == nil and t[4] == 'a')
                f(1,2,   -- this one too
                      3,4)
                xassert('extraparam', t[1] == 1 and t[2] == 2 and t[3] == 3 and t[4] == 'a')

                "
            );
        }

        [global::TUnit.Core.Test]
        public Task LuaSuiteCallsClosures()
        {
            return RunTestAsync(
                @"
                -- fixed-point operator
                Z = function (le)
                      local function a (f)
                        return le(function (x) return f(f)(x) end)
                      end
                      return a(a)
                    end


                -- non-recursive factorial

                F = function (f)
                      return function (n)
                               if n == 0 then return 1
                               else return n*f(n-1) end
                             end
                    end

                fat = Z(F)

                xassert('fat(0) == 1 and fat(4) == 24 and Z(F)(5)==5*Z(F)(4)', fat(0) == 1 and fat(4) == 24 and Z(F)(5)==5*Z(F)(4))

                local function g (z)
                  local function f (a,b,c,d)
                    return function (x,y) return a+b+c+d+a+x+y+z end
                  end
                  return f(z,z+1,z+2,z+3)
                end

                f = g(10)

                xassert('f(9, 16) == 10+11+12+13+10+9+16+10', f(9, 16) == 10+11+12+13+10+9+16+10)

                Z, F, f = nil
                --print('+')
                "
            );
        }

        private static async Task RunTestAsync(string script)
        {
            HashSet<string> failedTests = new(StringComparer.Ordinal);
            int assertIndex = 0;

            Script scriptHost = new();
            Table globals = scriptHost.Globals;

            globals.Set(
                DynValue.NewString("xassert"),
                DynValue.NewCallback(
                    new CallbackFunction(
                        (context, args) =>
                        {
                            if (!args[1].CastToBool())
                            {
                                failedTests.Add(args[0].String);
                            }

                            return DynValue.Nil;
                        }
                    )
                )
            );

            globals.Set(
                DynValue.NewString("assert"),
                DynValue.NewCallback(
                    new CallbackFunction(
                        (context, args) =>
                        {
                            ++assertIndex;

                            if (!args[0].CastToBool())
                            {
                                failedTests.Add($"assert #{assertIndex}");
                            }

                            return DynValue.Nil;
                        }
                    )
                )
            );

            globals.Set(
                DynValue.NewString("print"),
                DynValue.NewCallback(
                    new CallbackFunction(
                        (context, args) =>
                        {
                            return DynValue.Nil;
                        }
                    )
                )
            );

            try
            {
                scriptHost.DoString(script);
            }
            catch (ScriptRuntimeException ex)
            {
                throw new InvalidOperationException(
                    $"Lua test suite failure: {ex.DecoratedMessage}",
                    ex
                );
            }

            if (failedTests.Count > 0)
            {
                string details = string.Join(
                    ", ",
                    failedTests.OrderBy(entry => entry, StringComparer.Ordinal)
                );
                throw new InvalidOperationException($"Failed asserts {details}");
            }

            await Assert.That(failedTests.Count).IsEqualTo(0).ConfigureAwait(false);
        }
    }
}
