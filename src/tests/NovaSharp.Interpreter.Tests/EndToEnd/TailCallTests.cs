namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public class TailCallTests
    {
        [Test]
        public void TcoTestPre()
        {
            // this just verifies the algorithm for TcoTestBig
            string script =
                @"
				function recsum(num, partial)
					if (num == 0) then
						return partial
					else
						return recsum(num - 1, partial + num)
					end
				end
				
				return recsum(10, 0)";

            Script s = new();
            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(55));
        }

        [Test]
        public void TcoTestBig()
        {
            // calc the sum of the first N numbers in the most stupid way ever to waste stack and trigger TCO..
            // (this could be a simple X*(X+1) / 2... )
            string script =
                @"
				function recsum(num, partial)
					if (num == 0) then
						return partial
					else
						return recsum(num - 1, partial + num)
					end
				end
				
				return recsum(70000, 0)";

            Script s = new();
            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(2450035000.0));
        }

        [Test]
        public void TailCallFromClr()
        {
            string script =
                @"
				function getResult(x)
					return 156*x;  
				end

				return clrtail(9)";

            Script s = new();

            s.Globals.Set(
                "clrtail",
                DynValue.NewCallback(
                    (xc, a) =>
                    {
                        DynValue fn = s.Globals.Get("getResult");
                        DynValue k3 = DynValue.NewNumber(a[0].Number / 3);

                        return DynValue.NewTailCallReq(fn, k3);
                    }
                )
            );

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(468));
        }

        [Test]
        public void CheckToString()
        {
            string script =
                @"
				return tostring(9)";

            Script s = new(CoreModules.Basic);
            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("9"));
        }

        [Test]
        public void CheckToStringMeta()
        {
            string script =
                @"
				t = {}
				m = {
					__tostring = function(v)
						return 'ciao';
					end
				}

				setmetatable(t, m);
				s = tostring(t);

				return (s);";

            Script s = new();
            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("ciao"));
        }
    }
}
