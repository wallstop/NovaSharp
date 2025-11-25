namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    public enum SampleRating : short
    {
        Uno = 1,
        MenoUno = -1,
        Quattro = 4,
        Cinque = 5,
        TantaRoba = short.MaxValue,
        PocaRoba = short.MinValue,
    }

    [Flags]
    public enum SampleFlagSet : ushort
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
            public string MyMethod(SampleRating enm)
            {
                return "[" + enm.ToString() + "]";
            }

            public string MyMethod(SampleFlagSet enm)
            {
                return ((long)enm).ToString();
            }

            public string MyMethod2(SampleRating enm)
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
            public SampleRating Get()
            {
                return SampleRating.Quattro;
            }

            [SuppressMessage(
                "Design",
                "CA1024:UsePropertiesWhereAppropriate",
                Justification = "Enum interop coverage uses callable getters to validate overload resolution."
            )]
            public SampleFlagSet GetF()
            {
                return SampleFlagSet.Quattro;
            }
        }

        private void RunTestOverload(string code, string expected)
        {
            Script s = new();

            EnumOverloadsTestClass obj = new();

            UserData.RegisterType<EnumOverloadsTestClass>(InteropAccessMode.Reflection);

            UserData.RegisterType<SampleRating>();
            UserData.RegisterType<SampleFlagSet>();

            s.Globals.Set("SampleRating", UserData.CreateStatic<SampleRating>());
            //			S.Globals.Set("SampleFlagSet", UserData.CreateStatic<SampleFlagSet>());
            s.Globals["SampleFlagSet"] = typeof(SampleFlagSet);

            s.Globals.Set("o", UserData.Create(obj));

            DynValue v = s.DoString("return " + code);

            Assert.That(v.Type, Is.EqualTo(DataType.String));
            Assert.That(v.String, Is.EqualTo(expected));
        }

        [Test]
        public void InteropEnumSimple()
        {
            RunTestOverload("o:MyMethod2(SampleRating.Cinque)", "(Cinque)");
        }

        [Test]
        public void InteropEnumSimple2()
        {
            RunTestOverload("o:MyMethod2(SampleRating.cinque)", "(Cinque)");
        }

        [Test]
        public void InteropEnumOverload1()
        {
            RunTestOverload(
                "o:MyMethod(SampleFlagSet.FlagsOr(SampleFlagSet.Uno, SampleFlagSet.Due))",
                "3"
            );
            RunTestOverload("o:MyMethod(SampleRating.Cinque)", "[Cinque]");
        }

        [Test]
        public void InteropEnumNumberConversion()
        {
            RunTestOverload("o:MyMethod2(5)", "(Cinque)");
        }

        [Test]
        public void InteropEnumFlagsOr()
        {
            RunTestOverload(
                "o:MyMethod(SampleFlagSet.FlagsOr(SampleFlagSet.Uno, SampleFlagSet.Due))",
                "3"
            );
        }

        [Test]
        public void InteropEnumFlagsAnd()
        {
            RunTestOverload(
                "o:MyMethod(SampleFlagSet.FlagsAnd(SampleFlagSet.Uno, SampleFlagSet.Cinque))",
                "1"
            );
        }

        [Test]
        public void InteropEnumFlagsXor()
        {
            RunTestOverload(
                "o:MyMethod(SampleFlagSet.FlagsXor(SampleFlagSet.Uno, SampleFlagSet.Cinque))",
                "4"
            );
        }

        [Test]
        public void InteropEnumFlagsNot()
        {
            RunTestOverload(
                "o:MyMethod(SampleFlagSet.FlagsAnd(SampleFlagSet.Cinque, SampleFlagSet.FlagsNot(SampleFlagSet.Uno)))",
                "4"
            );
        }

        [Test]
        public void InteropEnumFlagsOr2()
        {
            RunTestOverload("o:MyMethod(SampleFlagSet.FlagsOr(SampleFlagSet.Uno, 2))", "3");
        }

        [Test]
        public void InteropEnumFlagsOr3()
        {
            RunTestOverload("o:MyMethod(SampleFlagSet.FlagsOr(1, SampleFlagSet.Due))", "3");
        }

        [Test]
        public void InteropEnumFlagsOrMeta()
        {
            RunTestOverload("o:MyMethod(SampleFlagSet.Uno .. SampleFlagSet.Due)", "3");
        }

        [Test]
        public void InteropEnumFlagsHasAll()
        {
            RunTestOverload(
                "o:MyMethodB(SampleFlagSet.hasAll(SampleFlagSet.Uno, SampleFlagSet.Cinque))",
                "F"
            );
            RunTestOverload(
                "o:MyMethodB(SampleFlagSet.hasAll(SampleFlagSet.Cinque, SampleFlagSet.Uno))",
                "T"
            );
        }

        [Test]
        public void InteropEnumFlagsHasAny()
        {
            RunTestOverload(
                "o:MyMethodB(SampleFlagSet.hasAny(SampleFlagSet.Uno, SampleFlagSet.Cinque))",
                "T"
            );
            RunTestOverload(
                "o:MyMethodB(SampleFlagSet.hasAny(SampleFlagSet.Cinque, SampleFlagSet.Uno))",
                "T"
            );
            RunTestOverload(
                "o:MyMethodB(SampleFlagSet.hasAny(SampleFlagSet.Quattro, SampleFlagSet.Uno))",
                "F"
            );
        }

        [Test]
        public void InteropEnumRead()
        {
            RunTestOverload("o:MyMethod(o:get())", "[Quattro]");
        }

        [Test]
        public void InteropEnumFlagsOrMetaRead()
        {
            RunTestOverload("o:MyMethod(o:getF() .. SampleFlagSet.Due)", "6");
        }
    }
}
