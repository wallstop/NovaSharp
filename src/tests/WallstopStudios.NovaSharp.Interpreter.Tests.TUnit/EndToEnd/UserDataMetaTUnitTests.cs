namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class UserDataMetaTUnitTests
    {
        internal sealed class ClassWithLength
        {
            private readonly int _length = 55;

            public int Length => _length;
        }

        internal sealed class ClassWithCount
        {
            private readonly int _count = 123;

            public int Count => _count;
        }

        internal sealed class ArithmOperatorsTestClass : IComparable, IEnumerable
        {
            public int Value { get; set; }

            public ArithmOperatorsTestClass() { }

            public ArithmOperatorsTestClass(int value)
            {
                Value = value;
            }

            public static ArithmOperatorsTestClass operator -(ArithmOperatorsTestClass instance)
            {
                return new ArithmOperatorsTestClass(-instance.Value);
            }

            [NovaSharpUserDataMetamethod("__concat")]
            [NovaSharpUserDataMetamethod("__pow")]
            public static int operator +(ArithmOperatorsTestClass instance, int value)
            {
                return instance.Value + value;
            }

            [NovaSharpUserDataMetamethod("__concat")]
            [NovaSharpUserDataMetamethod("__pow")]
            public static int operator +(int value, ArithmOperatorsTestClass instance)
            {
                return instance.Value + value;
            }

            [NovaSharpUserDataMetamethod("__concat")]
            [NovaSharpUserDataMetamethod("__pow")]
            public static int operator +(
                ArithmOperatorsTestClass first,
                ArithmOperatorsTestClass second
            )
            {
                return first.Value + second.Value;
            }

            public static int operator -(ArithmOperatorsTestClass instance, int value)
            {
                return instance.Value - value;
            }

            public static int operator -(int value, ArithmOperatorsTestClass instance)
            {
                return value - instance.Value;
            }

            public static int operator -(
                ArithmOperatorsTestClass first,
                ArithmOperatorsTestClass second
            )
            {
                return first.Value - second.Value;
            }

            public static int operator *(ArithmOperatorsTestClass instance, int value)
            {
                return instance.Value * value;
            }

            public static int operator *(int value, ArithmOperatorsTestClass instance)
            {
                return instance.Value * value;
            }

            public static int operator *(
                ArithmOperatorsTestClass first,
                ArithmOperatorsTestClass second
            )
            {
                return first.Value * second.Value;
            }

            public static int operator /(ArithmOperatorsTestClass instance, int value)
            {
                return instance.Value / value;
            }

            public static int operator /(int value, ArithmOperatorsTestClass instance)
            {
                return value / instance.Value;
            }

            public static int operator /(
                ArithmOperatorsTestClass first,
                ArithmOperatorsTestClass second
            )
            {
                return first.Value / second.Value;
            }

            public static int operator %(ArithmOperatorsTestClass instance, int value)
            {
                return instance.Value % value;
            }

            public static int operator %(int value, ArithmOperatorsTestClass instance)
            {
                return value % instance.Value;
            }

            public static int operator %(
                ArithmOperatorsTestClass first,
                ArithmOperatorsTestClass second
            )
            {
                return first.Value % second.Value;
            }

            public override bool Equals(object obj)
            {
                if (obj is double doubleValue)
                {
                    return doubleValue == Value;
                }

                if (obj is long longValue)
                {
                    return longValue == Value;
                }

                if (obj is int intValue)
                {
                    return intValue == Value;
                }

                ArithmOperatorsTestClass other = obj as ArithmOperatorsTestClass;
                return other != null && Value == other.Value;
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            public int CompareTo(object obj)
            {
                if (obj is double doubleValue)
                {
                    return Value.CompareTo((int)doubleValue);
                }

                if (obj is long longValue)
                {
                    return Value.CompareTo((int)longValue);
                }

                if (obj is int intValue)
                {
                    return Value.CompareTo(intValue);
                }

                ArithmOperatorsTestClass other = obj as ArithmOperatorsTestClass;
                if (other == null)
                {
                    return 1;
                }

                return Value.CompareTo(other.Value);
            }

            public IEnumerator GetEnumerator()
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
            public IEnumerator Pairs()
            {
                _ = Value;
                List<DynValue> tuples = new()
                {
                    DynValue.NewTuple(DynValue.NewString("a"), DynValue.NewString("A")),
                    DynValue.NewTuple(DynValue.NewString("b"), DynValue.NewString("B")),
                    DynValue.NewTuple(DynValue.NewString("c"), DynValue.NewString("C")),
                };
                return tuples.GetEnumerator();
            }
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaPairs()
        {
            Script script = new();
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<ArithmOperatorsTestClass>(ensureUnregistered: true);
            registrationScope.RegisterType<ArithmOperatorsTestClass>();
            script.Globals.Set("o", UserData.Create(new ArithmOperatorsTestClass(-5)));

            string lua =
                @"
                local str = ''
                for k,v in pairs(o) do
                    str = str .. k .. v;
                end

                return str;
                ";

            DynValue result = script.DoString(lua);
            await Assert.That(result.String).IsEqualTo("aAbBcC").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaIPairs()
        {
            Script script = new();
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<ArithmOperatorsTestClass>(ensureUnregistered: true);
            registrationScope.RegisterType<ArithmOperatorsTestClass>();
            script.Globals.Set("o", UserData.Create(new ArithmOperatorsTestClass(-5)));

            string lua =
                @"
                local str = ''
                for k,v in ipairs(o) do
                    str = str .. k .. v;
                end

                return str;
                ";

            DynValue result = script.DoString(lua);
            await Assert.That(result.String).IsEqualTo("aAbBcC").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaIterator()
        {
            Script script = new();
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<ArithmOperatorsTestClass>(ensureUnregistered: true);
            registrationScope.RegisterType<ArithmOperatorsTestClass>();
            script.Globals.Set("o", UserData.Create(new ArithmOperatorsTestClass(-5)));

            string lua =
                @"
                local sum = 0
                for i in o do
                    sum = sum + i
                end

                return sum;
                ";

            DynValue result = script.DoString(lua);
            await Assert.That(result.Number).IsEqualTo(6).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaOpLen()
        {
            Script script = new();
            using UserDataRegistrationScope registrationScope = UserDataRegistrationScope.Track(
                ensureUnregistered: true,
                typeof(ArithmOperatorsTestClass),
                typeof(ClassWithCount),
                typeof(ClassWithLength)
            );
            registrationScope.RegisterType<ArithmOperatorsTestClass>();
            registrationScope.RegisterType<ClassWithCount>();
            registrationScope.RegisterType<ClassWithLength>();

            script.Globals.Set("o1", UserData.Create(new ArithmOperatorsTestClass(5)));
            script.Globals.Set("o2", UserData.Create(new ClassWithCount()));
            script.Globals.Set("o3", UserData.Create(new ClassWithLength()));

            await Assert
                .That(script.DoString("return #o3").Number)
                .IsEqualTo(55)
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return #o2").Number)
                .IsEqualTo(123)
                .ConfigureAwait(false);

            Assert.Throws<ScriptRuntimeException>(() => script.DoString("return #o1"));
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaEquality()
        {
            Script script = new();
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<ArithmOperatorsTestClass>(ensureUnregistered: true);
            registrationScope.RegisterType<ArithmOperatorsTestClass>();

            script.Globals.Set("o1", UserData.Create(new ArithmOperatorsTestClass(5)));
            script.Globals.Set("o2", UserData.Create(new ArithmOperatorsTestClass(1)));
            script.Globals.Set("o3", UserData.Create(new ArithmOperatorsTestClass(5)));

            await Assert
                .That(script.DoString("return o1 == o1").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o1 != o2").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o1 == o3").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o2 != o3").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o1 == 5").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return 5 == o1").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o1 != 6").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return 6 != o1").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return 'xx' != o1").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o1 != 'xx'").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaComparisons()
        {
            Script script = new();
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<ArithmOperatorsTestClass>(ensureUnregistered: true);
            registrationScope.RegisterType<ArithmOperatorsTestClass>();

            script.Globals.Set("o1", UserData.Create(new ArithmOperatorsTestClass(1)));
            script.Globals.Set("o2", UserData.Create(new ArithmOperatorsTestClass(4)));

            await Assert
                .That(script.DoString("return o1 <= o1").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o1 <= o2").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o1 < o2").Boolean)
                .IsTrue()
                .ConfigureAwait(false);

            await Assert
                .That(script.DoString("return o2 > o1").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o2 >= o1").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o2 >= o2").Boolean)
                .IsTrue()
                .ConfigureAwait(false);

            await Assert
                .That(script.DoString("return o1 <= 4").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o1 < 4").Boolean)
                .IsTrue()
                .ConfigureAwait(false);

            await Assert
                .That(script.DoString("return 4 > o1").Boolean)
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return 4 >= o1").Boolean)
                .IsTrue()
                .ConfigureAwait(false);

            await Assert
                .That(script.DoString("return o1 > o2").Boolean)
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o1 >= o2").Boolean)
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o2 < o1").Boolean)
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return o2 <= o1").Boolean)
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public Task InteropMetaCall()
        {
            return OperatorTestAsync("return o()", 5, -5);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaOpUnm()
        {
            await OperatorTestAsync("return -o + 5", 5, 0).ConfigureAwait(false);
            await OperatorTestAsync("return -o + -o", 5, -10).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaOpAdd()
        {
            await OperatorTestAsync("return o + 5", 5, 10).ConfigureAwait(false);
            await OperatorTestAsync("return o + o", 5, 10).ConfigureAwait(false);
            await OperatorTestAsync("return 5 + o", 5, 10).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaOpConcat()
        {
            await OperatorTestAsync("return o .. 5", 5, 10).ConfigureAwait(false);
            await OperatorTestAsync("return o .. o", 5, 10).ConfigureAwait(false);
            await OperatorTestAsync("return 5 .. o", 5, 10).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaOpPow()
        {
            await OperatorTestAsync("return o ^ 5", 5, 10).ConfigureAwait(false);
            await OperatorTestAsync("return o ^ o", 5, 10).ConfigureAwait(false);
            await OperatorTestAsync("return 5 ^ o", 5, 10).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaOpSub()
        {
            await OperatorTestAsync("return o - 5", 2, -3).ConfigureAwait(false);
            await OperatorTestAsync("return o - o", 2, 0).ConfigureAwait(false);
            await OperatorTestAsync("return 5 - o", 2, 3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaOpMul()
        {
            await OperatorTestAsync("return o * 5", 3, 15).ConfigureAwait(false);
            await OperatorTestAsync("return o * o", 3, 9).ConfigureAwait(false);
            await OperatorTestAsync("return 5 * o", 3, 15).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaOpDiv()
        {
            await OperatorTestAsync("return o / 5", 25, 5).ConfigureAwait(false);
            await OperatorTestAsync("return o / o", 117, 1).ConfigureAwait(false);
            await OperatorTestAsync("return 15 / o", 5, 3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaOpMod()
        {
            await OperatorTestAsync("return o % 5", 16, 1).ConfigureAwait(false);
            await OperatorTestAsync("return o % o", 3, 0).ConfigureAwait(false);
            await OperatorTestAsync("return 5 % o", 3, 2).ConfigureAwait(false);
        }

        private static async Task OperatorTestAsync(string code, int input, int expected)
        {
            Script script = new();
            ArithmOperatorsTestClass target = new(input);

            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<ArithmOperatorsTestClass>(ensureUnregistered: true);
            registrationScope.RegisterType<ArithmOperatorsTestClass>();
            script.Globals.Set("o", UserData.Create(target));

            DynValue result = script.DoString(code);

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(expected).ConfigureAwait(false);
        }
    }
}
