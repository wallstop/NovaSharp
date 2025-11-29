#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Tests;

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

        internal sealed class ArithmOperatorsTestClass : IComparable, System.Collections.IEnumerable
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
                if (obj is double d)
                {
                    return d == Value;
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
                if (obj is double d)
                {
                    return Value.CompareTo((int)d);
                }

                ArithmOperatorsTestClass other = obj as ArithmOperatorsTestClass;
                if (other == null)
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
                _ = Value;
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

        [global::TUnit.Core.Test]
        public async Task InteropMetaPairs()
        {
            Script script = new();
            UserData.RegisterType<ArithmOperatorsTestClass>();
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
            await Assert.That(result.String).IsEqualTo("aAbBcC");
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaIPairs()
        {
            Script script = new();
            UserData.RegisterType<ArithmOperatorsTestClass>();
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
            await Assert.That(result.String).IsEqualTo("aAbBcC");
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaIterator()
        {
            Script script = new();
            UserData.RegisterType<ArithmOperatorsTestClass>();
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
            await Assert.That(result.Number).IsEqualTo(6);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaOpLen()
        {
            Script script = new();
            UserData.RegisterType<ArithmOperatorsTestClass>();
            UserData.RegisterType<ClassWithCount>();
            UserData.RegisterType<ClassWithLength>();

            script.Globals.Set("o1", UserData.Create(new ArithmOperatorsTestClass(5)));
            script.Globals.Set("o2", UserData.Create(new ClassWithCount()));
            script.Globals.Set("o3", UserData.Create(new ClassWithLength()));

            await Assert.That(script.DoString("return #o3").Number).IsEqualTo(55);
            await Assert.That(script.DoString("return #o2").Number).IsEqualTo(123);

            Assert.Catch<ScriptRuntimeException>(() => script.DoString("return #o1"));
        }

        [global::TUnit.Core.Test]
        public async Task InteropMetaEquality()
        {
            Script script = new();
            UserData.RegisterType<ArithmOperatorsTestClass>();

            script.Globals.Set("o1", UserData.Create(new ArithmOperatorsTestClass(5)));
            script.Globals.Set("o2", UserData.Create(new ArithmOperatorsTestClass(1)));
            script.Globals.Set("o3", UserData.Create(new ArithmOperatorsTestClass(5)));

            await Assert.That(script.DoString("return o1 == o1").Boolean).IsTrue();
            await Assert.That(script.DoString("return o1 != o2").Boolean).IsTrue();
            await Assert.That(script.DoString("return o1 == о3").Boolean).IsTrue();
        }
    }
}
#pragma warning restore CA2007
