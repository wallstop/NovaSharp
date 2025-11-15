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
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DispatchingUserDataDescriptorTests
    {
        private const string IndexerGetterName = "get_Item";
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
            UserData.RegisterType<CountOnlyHost>(InteropAccessMode.Reflection);
            UserData.RegisterType<IntConversionOnlyHost>(InteropAccessMode.Reflection);
            UserData.RegisterType<NoConversionHost>(InteropAccessMode.Reflection);
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
        public void ComparisonMetamethodSupportsSecondOperandOwnership()
        {
            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(hostAdd);
            DynValue meta = descriptor.MetaIndex(script, hostAdd, "__lt");
            Assert.That(meta, Is.Not.Null, "__lt metamethod should be registered");

            DynValue result = script.Call(
                meta,
                UserData.Create(hostOther),
                UserData.Create(hostAdd)
            );

            Assert.That(result.Boolean, Is.False);
        }

        [Test]
        public void LenOperatorReturnsCount()
        {
            DynValue len = script.DoString("return #hostAdd");
            Assert.That(len.Number, Is.EqualTo(2));
        }

        [Test]
        public void LenOperatorFallsBackToCountWhenLengthMissing()
        {
            script.Globals["countOnly"] = UserData.Create(new CountOnlyHost(4));

            DynValue len = script.DoString("return #countOnly");

            Assert.That(len.Number, Is.EqualTo(4));
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
        public void ToNumberSkipsMissingConvertersBeforeFindingMatch()
        {
            IntConversionOnlyHost host = new(9);
            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(host);
            DynValue meta = descriptor.MetaIndex(script, host, "__tonumber");

            Assert.That(meta, Is.Not.Null, "__tonumber meta should be generated for numeric conversions");

            DynValue number = script.Call(meta, UserData.Create(host));

            Assert.That(number.Type, Is.EqualTo(DataType.Number));
            Assert.That(number.Number, Is.EqualTo(9));
        }

        [Test]
        public void ToNumberReturnsNilWhenNoConvertersExist()
        {
            NoConversionHost host = new(6);
            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(host);
            DynValue meta = descriptor.MetaIndex(script, host, "__tonumber");

            Assert.That(meta, Is.Null);
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
        public void MemberManagementSupportsAddRemoveQueries()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            StubMemberDescriptor member = StubMemberDescriptor.CreateCallable(
                "Value",
                MemberDescriptorAccess.CanRead
            );
            descriptor.AddMember("Value", member);
            descriptor.AddMetaMember("__meta", member);
            descriptor.AddDynValue("dyn", DynValue.NewNumber(5));

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.HasMember("Value"), Is.True);
                Assert.That(descriptor.MemberNames, Does.Contain("Value"));
                Assert.That(descriptor.Members, Has.Some.Matches<KeyValuePair<string, IMemberDescriptor>>(kv => kv.Key == "dyn"));
                Assert.That(descriptor.MetaMemberNames, Does.Contain("__meta"));
                Assert.That(descriptor.HasMetaMember("__meta"), Is.True);
            });

            descriptor.RemoveMember("Value");
            descriptor.RemoveMetaMember("__meta");

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.HasMember("Value"), Is.False);
                Assert.That(descriptor.HasMetaMember("__meta"), Is.False);
            });
        }

        [Test]
        public void IndexerInvokesClrCallbackWhenAccessedFromBracketSyntax()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            bool invoked = false;
            descriptor.AddMember(
                IndexerGetterName,
                StubMemberDescriptor.CreateCallable(
                    IndexerGetterName,
                    MemberDescriptorAccess.CanExecute | MemberDescriptorAccess.CanRead,
                    (_, _) =>
                        DynValue.NewCallback(
                            (context, args) =>
                            {
                                invoked = true;
                                return DynValue.NewString($"idx:{args[0].Number}");
                            }
                        )
                )
            );

            Script localScript = new Script(CoreModules.None);
            DynValue result = descriptor.Index(
                localScript,
                new DescriptorHost(),
                DynValue.NewNumber(7),
                isDirectIndexing: false
            );

            Assert.Multiple(() =>
            {
                Assert.That(invoked, Is.True);
                Assert.That(result.String, Is.EqualTo("idx:7"));
            });
        }

        [Test]
        public void IndexerThrowsWhenGetterIsNotClrCallback()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            descriptor.AddMember(
                IndexerGetterName,
                StubMemberDescriptor.CreateCallable(
                    IndexerGetterName,
                    MemberDescriptorAccess.CanExecute | MemberDescriptorAccess.CanRead,
                    (_, _) => DynValue.NewString("not a callback")
                )
            );

            Script localScript = new Script(CoreModules.None);

            Assert.That(
                () => descriptor.Index(
                    localScript,
                    new DescriptorHost(),
                    DynValue.NewNumber(1),
                    isDirectIndexing: false
                ),
                Throws.InstanceOf<ScriptRuntimeException>()
                    .With.Message.Contains("clr callback was expected")
            );
        }

        [Test]
        public void IndexFallsBackToExtensionMethodsAfterRegistration()
        {
            UserData.RegisterExtensionType(typeof(DispatchHostExtensionMethods));

            DynValue result = script.DoString("return hostAdd:DescribeExt()");

            Assert.That(result.String, Is.EqualTo("ext:2"));
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

        private sealed class StubMemberDescriptor : IMemberDescriptor
        {
            private readonly Func<Script, object, DynValue> getter;

            private StubMemberDescriptor(
                string name,
                MemberDescriptorAccess access,
                bool isStatic,
                Func<Script, object, DynValue> getter
            )
            {
                Name = name;
                MemberAccess = access;
                IsStatic = isStatic;
                this.getter = getter ?? ((_, _) => DynValue.Nil);
            }

            public static StubMemberDescriptor CreateCallable(
                string name,
                MemberDescriptorAccess access,
                Func<Script, object, DynValue> getter = null
            )
            {
                return new StubMemberDescriptor(
                    name,
                    access,
                    isStatic: false,
                    getter: getter ?? ((_, _) => DynValue.NewCallback((_, _) => DynValue.Nil))
                );
            }

            public bool IsStatic { get; }

            public string Name { get; }

            public MemberDescriptorAccess MemberAccess { get; }

            public DynValue GetValue(Script script, object obj)
            {
                return getter(script, obj);
            }

            public void SetValue(Script script, object obj, DynValue value)
            {
                throw new NotSupportedException();
            }
        }

        private static DispatchingUserDataDescriptor CreateDescriptorHostDescriptor()
        {
            return new DescriptorHostDescriptor();
        }

        private sealed class DescriptorHostDescriptor
            : DispatchingUserDataDescriptor
        {
            public DescriptorHostDescriptor()
                : base(typeof(DescriptorHost)) { }
        }
    }

    internal sealed class DescriptorHost { }

    internal sealed class CountOnlyHost
    {
        public CountOnlyHost(int count)
        {
            Count = count;
        }

        public int Count { get; }
    }

    internal sealed class IntConversionOnlyHost
    {
        public IntConversionOnlyHost(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public static implicit operator int(IntConversionOnlyHost host)
        {
            return host.Value;
        }
    }

    internal sealed class NoConversionHost
    {
        public NoConversionHost(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    internal sealed class DispatchHost : IComparable<DispatchHost>, IComparable, IEnumerable<int>
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

    internal static class DispatchHostExtensionMethods
    {
        public static string DescribeExt(this DispatchHost host)
        {
            return $"ext:{host.Value}";
        }
    }
}
