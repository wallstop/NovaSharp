namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    public class GotoTests
    {
        [Test]
        public void GotoSimpleFwd()
        {
            string script =
                @"
				function test()
					x = 3
					goto skip	
					x = x + 2;
					::skip::
					return x;
				end				

				return test();
				";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(3));
        }

        [Test]
        public void GotoSimpleBwd()
        {
            string script =
                @"
				function test()
					x = 5;
	
					::jump::
					if (x == 3) then return x; end
					
					x = 3
					goto jump

					x = 4
					return x;
				end				

				return test();
				";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(3));
        }

        [Test]
        public void GotoUndefinedLabel()
        {
            string script =
                @"
				goto there
				";

            Assert.Throws<SyntaxErrorException>(() => Script.RunString(script));
        }

        [Test]
        public void GotoDoubleDefinedLabel()
        {
            string script =
                @"
				::label::
				::label::
				";

            Assert.Throws<SyntaxErrorException>(() => Script.RunString(script));
        }

        [Test]
        public void GotoRedefinedLabel()
        {
            string script =
                @"
				::label::
				do
					::label::
				end
				";

            Script.RunString(script);
        }

        [Test]
        public void GotoRedefinedLabelGoto()
        {
            string script =
                @"
				::label::
				do
					goto label
					do return 5 end
					::label::
					return 3
				end
				";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(3));
        }

        [Test]
        public void GotoUndefinedLabel2()
        {
            string script =
                @"
				goto label
				do
					do return 5 end
					::label::
					return 3
				end
				";

            Assert.Throws<SyntaxErrorException>(() => Script.RunString(script));
        }

        [Test]
        public void GotoVarInScope()
        {
            string script =
                @"
				goto f
				local x
				::f::
				";

            Assert.Throws<SyntaxErrorException>(() => Script.RunString(script));
        }

        [Test]
        public void GotoJumpOutOfBlocks()
        {
            string script =
                @"
				local u = 4

				do
					local x = 5
	
					do
						local y = 6
		
						do
							local z = 7
						end
		
						goto out
					end
				end

				do return 5 end

				::out::

				return 3
			";

            DynValue res = Script.RunString(script);
            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(3));
        }

        [Test]
        public void GotoJumpOutOfScopes()
        {
            string script =
                @"
				local u = 4

				do
					local x = 5
					do
						local y = 6
						do
							goto out
							local z = 7
						end
		
					end
				end

				::out::

				do 
					local a
					local b = 55

					if (a == nil) then
						b = b + 12
					end

					return b
				end

			";

            DynValue res = Script.RunString(script);
            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(67));
        }
    }
}
