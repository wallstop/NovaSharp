namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    [TestFixture]
    public class UserDataIndexerTests
    {
        public class IndexerTestClass
        {
            private readonly Dictionary<int, int> _mymap = new();

            public int this[int idx]
            {
                get { return _mymap[idx]; }
                set { _mymap[idx] = value; }
            }

            public int this[int idx1, int idx2, int idx3]
            {
                get
                {
                    int idx = (idx1 + idx2) * idx3;
                    return _mymap[idx];
                }
                set
                {
                    int idx = (idx1 + idx2) * idx3;
                    _mymap[idx] = value;
                }
            }
        }

        private void IndexerTest(string code, int expected)
        {
            Script s = new();

            IndexerTestClass obj = new();

            UserData.RegisterType<IndexerTestClass>();

            s.Globals.Set("o", UserData.Create(obj));

            DynValue v = s.DoString(code);

            Assert.That(v.Type, Is.EqualTo(DataType.Number));
            Assert.That(v.Number, Is.EqualTo(expected));
        }

        [Test]
        public void InteropSingleSetterOnly()
        {
            string script = @"o[1] = 1; return 13";
            IndexerTest(script, 13);
        }

        [Test]
        public void InteropSingleIndexerGetSet()
        {
            string script = @"o[5] = 19; return o[5];";
            IndexerTest(script, 19);
        }

        [Test]
        public void InteropMultiIndexerGetSet()
        {
            string script = @"o[1,2,3] = 47; return o[1,2,3];";
            IndexerTest(script, 47);
        }

        [Test]
        public void InteropMultiIndexerMetatableGetSet()
        {
            string script =
                @"
				m = { 
					__index = o,
					__newindex = o
				}

				t = { }

				setmetatable(t, m);

				t[10,11,12] = 1234; return t[10,11,12];";
            IndexerTest(script, 1234);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void InteropMultiIndexerMetamethodGetSet()
        {
            string script =
                @"
				m = { 
					__index = function() end,
					__newindex = function() end
				}

				t = { }

				setmetatable(t, m);

				t[10,11,12] = 1234; return t[10,11,12];";
            IndexerTest(script, 1234);
        }

        [Test]
        public void InteropMixedIndexerGetSet()
        {
            string script = @"o[3,2,3] = 119; return o[15];";
            IndexerTest(script, 119);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void InteropExpListIndexingCompilesButNotRun1()
        {
            string script =
                @"    
				x = { 99, 98, 97, 96 }				
				return x[2,3];
				";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(98));
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void InteropExpListIndexingCompilesButNotRun2()
        {
            string script =
                @"    
				x = { 99, 98, 97, 96 }				
				x[2,3] = 5;
				";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(98));
        }
    }
}
