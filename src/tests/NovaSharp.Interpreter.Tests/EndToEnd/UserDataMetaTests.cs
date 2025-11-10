namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NUnit.Framework;

    [TestFixture]
    public class UserDataMetaTests
    {
        internal sealed class ClassWithLength
        {
            public int Length
            {
                get { return 55; }
            }
        }

        internal sealed class ClassWithCount
        {
            public int Count
            {
                get { return 123; }
            }
        }

        internal sealed class ArithmOperatorsTestClass : IComparable, System.Collections.IEnumerable
        {
            public int Value { get; set; }

            public ArithmOperatorsTestClass() { }

            public ArithmOperatorsTestClass(int value)
            {
                Value = value;
            }

            public static ArithmOperatorsTestClass operator -(ArithmOperatorsTestClass o)
            {
                return new ArithmOperatorsTestClass(-o.Value);
            }

            [NovaSharpUserDataMetamethod("__concat")]
            [NovaSharpUserDataMetamethod("__pow")]
            public static int operator +(ArithmOperatorsTestClass o, int v)
            {
                return o.Value + v;
            }

            [NovaSharpUserDataMetamethod("__concat")]
            [NovaSharpUserDataMetamethod("__pow")]
            public static int operator +(int v, ArithmOperatorsTestClass o)
            {
                return o.Value + v;
            }

            [NovaSharpUserDataMetamethod("__concat")]
            [NovaSharpUserDataMetamethod("__pow")]
            public static int operator +(ArithmOperatorsTestClass o1, ArithmOperatorsTestClass o2)
            {
                return o1.Value + o2.Value;
            }

            public static int operator -(ArithmOperatorsTestClass o, int v)
            {
                return o.Value - v;
            }

            public static int operator -(int v, ArithmOperatorsTestClass o)
            {
                return v - o.Value;
            }

            public static int operator -(ArithmOperatorsTestClass o1, ArithmOperatorsTestClass o2)
            {
                return o1.Value - o2.Value;
            }

            public static int operator *(ArithmOperatorsTestClass o, int v)
            {
                return o.Value * v;
            }

            public static int operator *(int v, ArithmOperatorsTestClass o)
            {
                return o.Value * v;
            }

            public static int operator *(ArithmOperatorsTestClass o1, ArithmOperatorsTestClass o2)
            {
                return o1.Value * o2.Value;
            }

            public static int operator /(ArithmOperatorsTestClass o, int v)
            {
                return o.Value / v;
            }

            public static int operator /(int v, ArithmOperatorsTestClass o)
            {
                return v / o.Value;
            }

            public static int operator /(ArithmOperatorsTestClass o1, ArithmOperatorsTestClass o2)
            {
                return o1.Value / o2.Value;
            }

            public static int operator %(ArithmOperatorsTestClass o, int v)
            {
                return o.Value % v;
            }

            public static int operator %(int v, ArithmOperatorsTestClass o)
            {
                return v % o.Value;
            }

            public static int operator %(ArithmOperatorsTestClass o1, ArithmOperatorsTestClass o2)
            {
                return o1.Value % o2.Value;
            }

            public override bool Equals(object obj)
            {
                if (obj is double d)
                {
                    return d == Value;
                }

                if (obj is not ArithmOperatorsTestClass other)
                {
                    return false;
                }

                return Value == other.Value;
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            public int CompareTo(object obj)
            {
                if (obj is double d)
                {
                    return Value.CompareTo((int)d);
                }

                if (obj is not ArithmOperatorsTestClass other)
                {
                    return 1;
                }

                return Value.CompareTo(other.Value);
            }

            public System.Collections.IEnumerator GetEnumerator()
            {
                return (new List<int>() { 1, 2, 3 }).GetEnumerator();
            }

            [NovaSharpUserDataMetamethod("__call")]
            public int DefaultMethod()
            {
                return -Value;
            }

            [NovaSharpUserDataMetamethod("__pairs")]
            [NovaSharpUserDataMetamethod("__ipairs")]
            public System.Collections.IEnumerator Pairs()
            {
                return (
                    new List<DynValue>()
                    {
                        DynValue.NewTuple(DynValue.NewString("a"), DynValue.NewString("A")),
                        DynValue.NewTuple(DynValue.NewString("b"), DynValue.NewString("B")),
                        DynValue.NewTuple(DynValue.NewString("c"), DynValue.NewString("C")),
                    }
                ).GetEnumerator();
            }
        }

        [Test]
        public void InteropMetaPairs()
        {
            Script s = new();
            UserData.RegisterType<ArithmOperatorsTestClass>();
            s.Globals.Set("o", UserData.Create(new ArithmOperatorsTestClass(-5)));

            string @script =
                @"
				local str = ''
				for k,v in pairs(o) do
					str = str .. k .. v;
				end

				return str;
				";

            Assert.That(s.DoString(script).String, Is.EqualTo("aAbBcC"));
        }

        [Test]
        public void InteropMetaIPairs()
        {
            Script s = new();
            UserData.RegisterType<ArithmOperatorsTestClass>();
            s.Globals.Set("o", UserData.Create(new ArithmOperatorsTestClass(-5)));

            string @script =
                @"
				local str = ''
				for k,v in ipairs(o) do
					str = str .. k .. v;
				end

				return str;
				";

            Assert.That(s.DoString(script).String, Is.EqualTo("aAbBcC"));
        }

        [Test]
        public void InteropMetaIterator()
        {
            Script s = new();
            UserData.RegisterType<ArithmOperatorsTestClass>();
            s.Globals.Set("o", UserData.Create(new ArithmOperatorsTestClass(-5)));

            string @script =
                @"
				local sum = 0
				for i in o do
					sum = sum + i
				end

				return sum;
				";

            Assert.That(s.DoString(script).Number, Is.EqualTo(6));
        }

        [Test]
        public void InteropMetaOpLen()
        {
            Script s = new();
            UserData.RegisterType<ArithmOperatorsTestClass>();
            UserData.RegisterType<ClassWithCount>();
            UserData.RegisterType<ClassWithLength>();

            s.Globals.Set("o1", UserData.Create(new ArithmOperatorsTestClass(5)));
            s.Globals.Set("o2", UserData.Create(new ClassWithCount()));
            s.Globals.Set("o3", UserData.Create(new ClassWithLength()));

            Assert.That(s.DoString("return #o3").Number, Is.EqualTo(55));
            Assert.That(s.DoString("return #o2").Number, Is.EqualTo(123));

            Assert.Catch<ScriptRuntimeException>(() => s.DoString("return #o1"));
        }

        [Test]
        public void InteropMetaEquality()
        {
            Script s = new();
            UserData.RegisterType<ArithmOperatorsTestClass>();

            s.Globals.Set("o1", UserData.Create(new ArithmOperatorsTestClass(5)));
            s.Globals.Set("o2", UserData.Create(new ArithmOperatorsTestClass(1)));
            s.Globals.Set("o3", UserData.Create(new ArithmOperatorsTestClass(5)));

            Assert.That(s.DoString("return o1 == o1").Boolean, Is.True, "o1 == o1");
            Assert.That(s.DoString("return o1 != o2").Boolean, Is.True, "o1 != o2");
            Assert.That(s.DoString("return o1 == o3").Boolean, Is.True, "o1 == o3");
            Assert.That(s.DoString("return o2 != o3").Boolean, Is.True, "o2 != o3");
            Assert.That(s.DoString("return o1 == 5 ").Boolean, Is.True, "o1 == 5 ");
            Assert.That(s.DoString("return 5 == o1 ").Boolean, Is.True, "5 == o1 ");
            Assert.That(s.DoString("return o1 != 6 ").Boolean, Is.True, "o1 != 6 ");
            Assert.That(s.DoString("return 6 != o1 ").Boolean, Is.True, "6 != o1 ");
            Assert.That(s.DoString("return 'xx' != o1 ").Boolean, Is.True, "'xx' != o1 ");
            Assert.That(s.DoString("return o1 != 'xx' ").Boolean, Is.True, "o1 != 'xx'");
        }

        [Test]
        public void InteropMetaComparisons()
        {
            Script s = new();
            UserData.RegisterType<ArithmOperatorsTestClass>();

            s.Globals.Set("o1", UserData.Create(new ArithmOperatorsTestClass(1)));
            s.Globals.Set("o2", UserData.Create(new ArithmOperatorsTestClass(4)));

            Assert.That(s.DoString("return o1 <= o1").Boolean, Is.True, "o1 <= o1");
            Assert.That(s.DoString("return o1 <= o2").Boolean, Is.True, "o1 <= o2");
            Assert.That(s.DoString("return o1 <  o2").Boolean, Is.True, "o1 <  o2");

            Assert.That(s.DoString("return o2 > o1 ").Boolean, Is.True, "o2 > o1 ");
            Assert.That(s.DoString("return o2 >= o1").Boolean, Is.True, "o2 >= o1");
            Assert.That(s.DoString("return o2 >= o2").Boolean, Is.True, "o2 >= o2");

            Assert.That(s.DoString("return o1 <= 4 ").Boolean, Is.True, "o1 <= 4 ");
            Assert.That(s.DoString("return o1 <  4 ").Boolean, Is.True, "o1 <  4 ");

            Assert.That(s.DoString("return 4 > o1  ").Boolean, Is.True, "4 > o1  ");
            Assert.That(s.DoString("return 4 >= o1 ").Boolean, Is.True, "4 >= o1 ");

            Assert.That(s.DoString("return o1 > o2 ").Boolean, Is.False, "o1 > o2 ");
            Assert.That(s.DoString("return o1 >= o2").Boolean, Is.False, "o1 >= o2");
            Assert.That(s.DoString("return o2 < o1 ").Boolean, Is.False, "o2 < o1 ");
            Assert.That(s.DoString("return o2 <= o1").Boolean, Is.False, "o2 <= o1");
        }

        private void OperatorTest(string code, int input, int output)
        {
            Script s = new();

            ArithmOperatorsTestClass obj = new(input);

            UserData.RegisterType<ArithmOperatorsTestClass>();

            s.Globals.Set("o", UserData.Create(obj));

            DynValue v = s.DoString(code);

            Assert.That(v.Type, Is.EqualTo(DataType.Number));
            Assert.That(v.Number, Is.EqualTo(output));
        }

        [Test]
        public void InteropMetaCall()
        {
            OperatorTest("return o()", 5, -5);
        }

        [Test]
        public void InteropMetaOpUnm()
        {
            OperatorTest("return -o + 5", 5, 0);
            OperatorTest("return -o + -o", 5, -10);
        }

        [Test]
        public void InteropMetaOpAdd()
        {
            OperatorTest("return o + 5", 5, 10);
            OperatorTest("return o + o", 5, 10);
            OperatorTest("return 5 + o", 5, 10);
        }

        [Test]
        public void InteropMetaOpConcat()
        {
            OperatorTest("return o .. 5", 5, 10);
            OperatorTest("return o .. o", 5, 10);
            OperatorTest("return 5 .. o", 5, 10);
        }

        [Test]
        public void InteropMetaOpPow()
        {
            OperatorTest("return o ^ 5", 5, 10);
            OperatorTest("return o ^ o", 5, 10);
            OperatorTest("return 5 ^ o", 5, 10);
        }

        [Test]
        public void InteropMetaOpSub()
        {
            OperatorTest("return o - 5", 2, -3);
            OperatorTest("return o - o", 2, 0);
            OperatorTest("return 5 - o", 2, 3);
        }

        [Test]
        public void InteropMetaOpMul()
        {
            OperatorTest("return o * 5", 3, 15);
            OperatorTest("return o * o", 3, 9);
            OperatorTest("return 5 * o", 3, 15);
        }

        [Test]
        public void InteropMetaOpDiv()
        {
            OperatorTest("return o / 5", 25, 5);
            OperatorTest("return o / o", 117, 1);
            OperatorTest("return 15 / o", 5, 3);
        }

        [Test]
        public void InteropMetaOpMod()
        {
            OperatorTest("return o % 5", 16, 1);
            OperatorTest("return o % o", 3, 0);
            OperatorTest("return 5 % o", 3, 2);
        }
    }
}
