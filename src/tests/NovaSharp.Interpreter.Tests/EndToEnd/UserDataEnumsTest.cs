namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    public enum MyEnum : short
    {
        Uno = 1,
        MenoUno = -1,
        Quattro = 4,
        Cinque = 5,
        TantaRoba = short.MaxValue,
        PocaRoba = short.MinValue,
    }

    [Flags]
    public enum MyFlags : ushort
    {
        Uno = 1,
        Due = 2,
        Quattro = 4,
        Cinque = 5,
        Otto = 8,
    }

    [TestFixture]
    public class UserDataEnumsTests
    {
        public class EnumOverloadsTestClass
        {
            public string MyMethod(MyEnum enm)
            {
                return "[" + enm.ToString() + "]";
            }

            public string MyMethod(MyFlags enm)
            {
                return ((long)enm).ToString();
            }

            public string MyMethod2(MyEnum enm)
            {
                return "(" + enm.ToString() + ")";
            }

            public string MyMethodB(bool b)
            {
                return b ? "T" : "F";
            }

            [SuppressMessage(
                "Design",
                "CA1024:UsePropertiesWhereAppropriate",
                Justification = "Enum interop coverage uses callable getters to validate overload resolution."
            )]
            public MyEnum Get()
            {
                return MyEnum.Quattro;
            }

            [SuppressMessage(
                "Design",
                "CA1024:UsePropertiesWhereAppropriate",
                Justification = "Enum interop coverage uses callable getters to validate overload resolution."
            )]
            public MyFlags GetF()
            {
                return MyFlags.Quattro;
            }
        }

        private void RunTestOverload(string code, string expected)
        {
            Script s = new();

            EnumOverloadsTestClass obj = new();

            UserData.RegisterType<EnumOverloadsTestClass>(InteropAccessMode.Reflection);

            UserData.RegisterType<MyEnum>();
            UserData.RegisterType<MyFlags>();

            s.Globals.Set("MyEnum", UserData.CreateStatic<MyEnum>());
            //			S.Globals.Set("MyFlags", UserData.CreateStatic<MyFlags>());
            s.Globals["MyFlags"] = typeof(MyFlags);

            s.Globals.Set("o", UserData.Create(obj));

            DynValue v = s.DoString("return " + code);

            Assert.That(v.Type, Is.EqualTo(DataType.String));
            Assert.That(v.String, Is.EqualTo(expected));
        }

        [Test]
        public void InteropEnumSimple()
        {
            RunTestOverload("o:MyMethod2(MyEnum.Cinque)", "(Cinque)");
        }

        [Test]
        public void InteropEnumSimple2()
        {
            RunTestOverload("o:MyMethod2(MyEnum.cinque)", "(Cinque)");
        }

        [Test]
        public void InteropEnumOverload1()
        {
            RunTestOverload("o:MyMethod(MyFlags.FlagsOr(MyFlags.Uno, MyFlags.Due))", "3");
            RunTestOverload("o:MyMethod(MyEnum.Cinque)", "[Cinque]");
        }

        [Test]
        public void InteropEnumNumberConversion()
        {
            RunTestOverload("o:MyMethod2(5)", "(Cinque)");
        }

        [Test]
        public void InteropEnumFlagsOr()
        {
            RunTestOverload("o:MyMethod(MyFlags.FlagsOr(MyFlags.Uno, MyFlags.Due))", "3");
        }

        [Test]
        public void InteropEnumFlagsAnd()
        {
            RunTestOverload("o:MyMethod(MyFlags.FlagsAnd(MyFlags.Uno, MyFlags.Cinque))", "1");
        }

        [Test]
        public void InteropEnumFlagsXor()
        {
            RunTestOverload("o:MyMethod(MyFlags.FlagsXor(MyFlags.Uno, MyFlags.Cinque))", "4");
        }

        [Test]
        public void InteropEnumFlagsNot()
        {
            RunTestOverload(
                "o:MyMethod(MyFlags.FlagsAnd(MyFlags.Cinque, MyFlags.FlagsNot(MyFlags.Uno)))",
                "4"
            );
        }

        [Test]
        public void InteropEnumFlagsOr2()
        {
            RunTestOverload("o:MyMethod(MyFlags.FlagsOr(MyFlags.Uno, 2))", "3");
        }

        [Test]
        public void InteropEnumFlagsOr3()
        {
            RunTestOverload("o:MyMethod(MyFlags.FlagsOr(1, MyFlags.Due))", "3");
        }

        [Test]
        public void InteropEnumFlagsOrMeta()
        {
            RunTestOverload("o:MyMethod(MyFlags.Uno .. MyFlags.Due)", "3");
        }

        [Test]
        public void InteropEnumFlagsHasAll()
        {
            RunTestOverload("o:MyMethodB(MyFlags.hasAll(MyFlags.Uno, MyFlags.Cinque))", "F");
            RunTestOverload("o:MyMethodB(MyFlags.hasAll(MyFlags.Cinque, MyFlags.Uno))", "T");
        }

        [Test]
        public void InteropEnumFlagsHasAny()
        {
            RunTestOverload("o:MyMethodB(MyFlags.hasAny(MyFlags.Uno, MyFlags.Cinque))", "T");
            RunTestOverload("o:MyMethodB(MyFlags.hasAny(MyFlags.Cinque, MyFlags.Uno))", "T");
            RunTestOverload("o:MyMethodB(MyFlags.hasAny(MyFlags.Quattro, MyFlags.Uno))", "F");
        }

        [Test]
        public void InteropEnumRead()
        {
            RunTestOverload("o:MyMethod(o:get())", "[Quattro]");
        }

        [Test]
        public void InteropEnumFlagsOrMetaRead()
        {
            RunTestOverload("o:MyMethod(o:getF() .. MyFlags.Due)", "6");
        }
    }
}
