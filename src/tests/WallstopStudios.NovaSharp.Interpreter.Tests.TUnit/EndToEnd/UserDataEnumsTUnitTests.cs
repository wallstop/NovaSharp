namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    public enum SampleRating
    {
        None = 0,
        Uno = 1,
        MenoUno = -1,
        Quattro = 4,
        Cinque = 5,
        TantaRoba = short.MaxValue,
        PocaRoba = short.MinValue,
    }

    [Flags]
    public enum SampleFlagSet
    {
        None = 0,
        Uno = 1,
        Due = 2,
        Quattro = 4,
        Cinque = 5,
        Otto = 8,
    }

    [UserDataIsolation]
    public sealed class UserDataEnumsTUnitTests
    {
        internal sealed class EnumOverloadsTestClass
        {
            private int _callCount;

            private void RecordCall()
            {
                _callCount = (_callCount + 1) % int.MaxValue;
            }

            public string MyMethod(SampleRating enm)
            {
                RecordCall();
                return "[" + enm.ToString() + "]";
            }

            public string MyMethod(SampleFlagSet enm)
            {
                RecordCall();
                return ((long)enm).ToString(CultureInfo.InvariantCulture);
            }

            public string MyMethod2(SampleRating enm)
            {
                RecordCall();
                return "(" + enm.ToString() + ")";
            }

            public string MyMethodB(bool value)
            {
                RecordCall();
                return value ? "T" : "F";
            }

            public SampleRating DefaultRating
            {
                get
                {
                    RecordCall();
                    return SampleRating.Quattro;
                }
            }

            public SampleFlagSet DefaultFlagSet
            {
                get
                {
                    RecordCall();
                    return SampleFlagSet.Quattro;
                }
            }
        }

        private static Task RunTestOverloadAsync(string code, string expected)
        {
            Script script = new();
            EnumOverloadsTestClass target = new();

            using UserDataRegistrationScope registrationScope = UserDataRegistrationScope.Track(
                ensureUnregistered: true,
                typeof(EnumOverloadsTestClass),
                typeof(SampleRating),
                typeof(SampleFlagSet)
            );

            registrationScope.RegisterType<EnumOverloadsTestClass>(
                InteropAccessMode.Reflection,
                ensureUnregistered: true
            );
            registrationScope.RegisterType<SampleRating>();
            registrationScope.RegisterType<SampleFlagSet>();

            script.Globals.Set("SampleRating", UserData.CreateStatic<SampleRating>());
            script.Globals["SampleFlagSet"] = typeof(SampleFlagSet);
            script.Globals.Set("o", UserData.Create(target));

            DynValue value = script.DoString("return " + code);

            return VerifyAsync(value);

            async Task VerifyAsync(DynValue result)
            {
                await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
                await Assert.That(result.String).IsEqualTo(expected).ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public Task InteropEnumSimple()
        {
            return RunTestOverloadAsync("o:MyMethod2(SampleRating.Cinque)", "(Cinque)");
        }

        [global::TUnit.Core.Test]
        public Task InteropEnumSimple2()
        {
            return RunTestOverloadAsync("o:MyMethod2(SampleRating.cinque)", "(Cinque)");
        }

        [global::TUnit.Core.Test]
        public async Task InteropEnumOverload1()
        {
            await RunTestOverloadAsync(
                    "o:MyMethod(SampleFlagSet.FlagsOr(SampleFlagSet.Uno, SampleFlagSet.Due))",
                    "3"
                )
                .ConfigureAwait(false);
            await RunTestOverloadAsync("o:MyMethod(SampleRating.Cinque)", "[Cinque]")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public Task InteropEnumNumberConversion()
        {
            return RunTestOverloadAsync("o:MyMethod2(5)", "(Cinque)");
        }

        [global::TUnit.Core.Test]
        public Task InteropEnumFlagsOr()
        {
            return RunTestOverloadAsync(
                "o:MyMethod(SampleFlagSet.FlagsOr(SampleFlagSet.Uno, SampleFlagSet.Due))",
                "3"
            );
        }

        [global::TUnit.Core.Test]
        public Task InteropEnumFlagsAnd()
        {
            return RunTestOverloadAsync(
                "o:MyMethod(SampleFlagSet.FlagsAnd(SampleFlagSet.Uno, SampleFlagSet.Cinque))",
                "1"
            );
        }

        [global::TUnit.Core.Test]
        public Task InteropEnumFlagsXor()
        {
            return RunTestOverloadAsync(
                "o:MyMethod(SampleFlagSet.FlagsXor(SampleFlagSet.Uno, SampleFlagSet.Cinque))",
                "4"
            );
        }

        [global::TUnit.Core.Test]
        public Task InteropEnumFlagsNot()
        {
            return RunTestOverloadAsync(
                "o:MyMethod(SampleFlagSet.FlagsAnd(SampleFlagSet.Cinque, SampleFlagSet.FlagsNot(SampleFlagSet.Uno)))",
                "4"
            );
        }

        [global::TUnit.Core.Test]
        public Task InteropEnumFlagsOr2()
        {
            return RunTestOverloadAsync(
                "o:MyMethod(SampleFlagSet.FlagsOr(SampleFlagSet.Uno, 2))",
                "3"
            );
        }

        [global::TUnit.Core.Test]
        public Task InteropEnumFlagsOr3()
        {
            return RunTestOverloadAsync(
                "o:MyMethod(SampleFlagSet.FlagsOr(1, SampleFlagSet.Due))",
                "3"
            );
        }

        [global::TUnit.Core.Test]
        public Task InteropEnumFlagsOrMeta()
        {
            return RunTestOverloadAsync("o:MyMethod(SampleFlagSet.Uno .. SampleFlagSet.Due)", "3");
        }

        [global::TUnit.Core.Test]
        public async Task InteropEnumFlagsHasAll()
        {
            await RunTestOverloadAsync(
                    "o:MyMethodB(SampleFlagSet.hasAll(SampleFlagSet.Uno, SampleFlagSet.Cinque))",
                    "F"
                )
                .ConfigureAwait(false);
            await RunTestOverloadAsync(
                    "o:MyMethodB(SampleFlagSet.hasAll(SampleFlagSet.Cinque, SampleFlagSet.Uno))",
                    "T"
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropEnumFlagsHasAny()
        {
            await RunTestOverloadAsync(
                    "o:MyMethodB(SampleFlagSet.hasAny(SampleFlagSet.Uno, SampleFlagSet.Cinque))",
                    "T"
                )
                .ConfigureAwait(false);
            await RunTestOverloadAsync(
                    "o:MyMethodB(SampleFlagSet.hasAny(SampleFlagSet.Cinque, SampleFlagSet.Uno))",
                    "T"
                )
                .ConfigureAwait(false);
            await RunTestOverloadAsync(
                    "o:MyMethodB(SampleFlagSet.hasAny(SampleFlagSet.Quattro, SampleFlagSet.Uno))",
                    "F"
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public Task InteropEnumRead()
        {
            return RunTestOverloadAsync("o:MyMethod(o.DefaultRating)", "[Quattro]");
        }

        [global::TUnit.Core.Test]
        public Task InteropEnumFlagsOrMetaRead()
        {
            return RunTestOverloadAsync("o:MyMethod(o.DefaultFlagSet .. SampleFlagSet.Due)", "6");
        }
    }
}
