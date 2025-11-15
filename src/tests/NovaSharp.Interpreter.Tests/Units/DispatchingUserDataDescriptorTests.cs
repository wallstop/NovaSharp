namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DispatchingUserDataDescriptorTests
    {
        private Script script = null!;
        private DispatchHost hostAdd = null!;
        private DispatchHost hostOther = null!;
        private DispatchHost hostCopy = null!;
        private DispatchHost hostZero = null!;

        [OneTimeSetUp]
        public void RegisterType()
        {
            UserData.RegisterType<DispatchHost>(InteropAccessMode.Reflection);
            UserData.RegisterType<MetaFallbackHost>(InteropAccessMode.Reflection);
        }

        [SetUp]
        public void CreateScript()
        {
            script = new Script(CoreModules.PresetComplete);
            hostAdd = new DispatchHost(2, new[] { 1, 2 });
            hostOther = new DispatchHost(3, new[] { 3 });
            hostCopy = new DispatchHost(2, new[] { 2 });
            hostZero = new DispatchHost(0, new[] { 0 });

            script.Globals["hostAdd"] = UserData.Create(hostAdd);
            script.Globals["hostOther"] = UserData.Create(hostOther);
            script.Globals["hostCopy"] = UserData.Create(hostCopy);
            script.Globals["hostZero"] = UserData.Create(hostZero);
        }

        [Test]
        public void ArithmeticOperatorsDispatchToClrOverloads()
        {
            DynValue sum = script.DoString("return (hostAdd + hostOther).value");
            DynValue difference = script.DoString("return (hostOther - hostAdd).value");
            DynValue product = script.DoString("return (hostAdd * hostOther).value");
            DynValue quotient = script.DoString("return (hostOther / hostAdd).value");
            DynValue modulus = script.DoString("return (hostOther % hostAdd).value");
            DynValue negate = script.DoString("return (-hostAdd).value");

            Assert.Multiple(() =>
            {
                Assert.That(sum.Number, Is.EqualTo(5));
                Assert.That(difference.Number, Is.EqualTo(1));
                Assert.That(product.Number, Is.EqualTo(6));
                Assert.That(quotient.Number, Is.EqualTo(1));
                Assert.That(modulus.Number, Is.EqualTo(1));
                Assert.That(negate.Number, Is.EqualTo(-2));
            });
        }

        [Test]
        public void ComparisonOperatorsUseComparable()
        {
            DynValue equality = script.DoString("return hostAdd == hostCopy");
            DynValue lessThan = script.DoString("return hostAdd < hostOther");
            DynValue lessThanOrEqual = script.DoString("return hostAdd <= hostCopy");

            Assert.Multiple(() =>
            {
                Assert.That(equality.Boolean, Is.True);
                Assert.That(lessThan.Boolean, Is.True);
                Assert.That(lessThanOrEqual.Boolean, Is.True);
            });
        }

        [Test]
        public void LenOperatorReturnsCount()
        {
            DynValue len = script.DoString("return #hostAdd");
            Assert.That(len.Number, Is.EqualTo(2));
        }

        [Test]
        public void ToNumberDispatchesImplicitConversion()
        {
            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(hostOther);
            DynValue meta = descriptor.MetaIndex(script, hostOther, "__tonumber");
            Assert.That(meta, Is.Not.Null, "__tonumber meta should be registered");

            DynValue number = script.Call(meta, UserData.Create(hostOther));

            Assert.That(number.Type, Is.EqualTo(DataType.Number));
            Assert.That(number.Number, Is.EqualTo(3));
        }

        [Test]
        public void ToBoolUsesImplicitConversion()
        {
            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(hostAdd);
            DynValue meta = descriptor.MetaIndex(script, hostAdd, "__tobool");
            Assert.That(meta, Is.Not.Null, "__tobool meta should be registered");

            DynValue trueResult = script.Call(meta, UserData.Create(hostAdd));
            DynValue falseResult = script.Call(meta, UserData.Create(hostZero));

            Assert.Multiple(() =>
            {
                Assert.That(trueResult.Boolean, Is.True);
                Assert.That(falseResult.Boolean, Is.False);
            });
        }

        [Test]
        public void IteratorDispatchEnumeratesClrEnumerable()
        {
            DynValue sum = script.DoString(
                @"
                local total = 0
                for value in hostAdd do
                    total = total + value
                end
                return total"
            );

            Assert.That(sum.Number, Is.EqualTo(3));
        }

        [Test]
        public void DivisionByZeroPropagatesInvocationException()
        {
            Assert.That(
                () => script.DoString("return (hostAdd / hostZero).value"),
                Throws.InstanceOf<TargetInvocationException>().With.InnerException.InstanceOf<DivideByZeroException>()
            );
        }

        [Test]
        public void ConcatOperatorFallsBackToRegisteredMetamethod()
        {
            script.Globals["concatLeft"] = UserData.Create(new MetaFallbackHost("L"));
            script.Globals["concatRight"] = UserData.Create(new MetaFallbackHost("R"));

            DynValue concat = script.DoString("return concatLeft .. concatRight");

            Assert.That(concat.String, Is.EqualTo("L|R"));
        }

        private sealed class DispatchHost : IComparable<DispatchHost>, IComparable, IEnumerable<int>
        {
            private readonly int value;
            private readonly IList<int> sequence;

            public DispatchHost(int value, IEnumerable<int> sequence)
            {
                this.value = value;
                this.sequence = new List<int>(sequence);
            }

            public int Value => value;

            public int Length => sequence.Count;

            public int Count => sequence.Count;

            public static DispatchHost operator +(DispatchHost left, DispatchHost right)
            {
                return new DispatchHost(left.value + right.value, left.sequence);
            }

            public static DispatchHost operator -(DispatchHost left, DispatchHost right)
            {
                return new DispatchHost(left.value - right.value, left.sequence);
            }

            public static DispatchHost operator *(DispatchHost left, DispatchHost right)
            {
                return new DispatchHost(left.value * right.value, left.sequence);
            }

            public static DispatchHost operator /(DispatchHost left, DispatchHost right)
            {
                return new DispatchHost(left.value / right.value, left.sequence);
            }

            public static DispatchHost operator %(DispatchHost left, DispatchHost right)
            {
                return new DispatchHost(left.value % right.value, left.sequence);
            }

            public static DispatchHost operator -(DispatchHost host)
            {
                return new DispatchHost(-host.value, host.sequence);
            }

            public override bool Equals(object obj)
            {
                return obj is DispatchHost other && other.value == value;
            }

            public override int GetHashCode()
            {
                return value;
            }

            public int CompareTo(DispatchHost other)
            {
                return value.CompareTo(other?.value ?? 0);
            }

            int IComparable.CompareTo(object obj)
            {
                if (obj is DispatchHost other)
                {
                    return CompareTo(other);
                }

                throw new ArgumentException("Object must be a DispatchHost", nameof(obj));
            }

            public IEnumerator<int> GetEnumerator()
            {
                return sequence.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public static implicit operator double(DispatchHost host)
            {
                return host.value;
            }

            public static implicit operator bool(DispatchHost host)
            {
                return host.value != 0;
            }
        }

        private sealed class MetaFallbackHost
        {
            public MetaFallbackHost(string label)
            {
                Label = label;
            }

            public string Label { get; }

            [NovaSharpUserDataMetamethod("__concat")]
            public static string Concat(MetaFallbackHost left, MetaFallbackHost right)
            {
                return $"{left.Label}|{right.Label}";
            }
        }
    }
}
