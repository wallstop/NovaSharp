namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DispatchingUserDataDescriptorTests
    {
        private const string IndexerGetterName = "get_Item";
        private const string IndexerSetterName = "set_Item";
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
        public void MetaIndexReturnsNullForUnsupportedMetamethod()
        {
            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(hostAdd);

            DynValue meta = descriptor.MetaIndex(script, hostAdd, "__unknown");

            Assert.That(meta, Is.Null);
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
        public void ComparisonMetamethodReturnsNullWhenObjectIsNotComparable()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            DynValue lt = descriptor.MetaIndex(script, new DescriptorHost(), "__lt");
            DynValue le = descriptor.MetaIndex(script, new DescriptorHost(), "__le");

            Assert.Multiple(() =>
            {
                Assert.That(lt, Is.Null);
                Assert.That(le, Is.Null);
            });
        }

        [Test]
        public void LenMetamethodReturnsNullWhenTargetIsNull()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();

            DynValue len = descriptor.MetaIndex(script, null, "__len");

            Assert.That(len, Is.Null);
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
                Assert.That(
                    descriptor.MetaMembers,
                    Has.Some.Matches<KeyValuePair<string, IMemberDescriptor>>(kv =>
                        kv.Key == "__meta" && kv.Value == member
                    )
                );
                Assert.That(descriptor.FindMetaMember("__meta"), Is.SameAs(member));
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
        public void AddMemberThrowsWhenDuplicateNonOverloadAdded()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            descriptor.AddMember(
                "Duplicate",
                StubMemberDescriptor.CreateCallable("Duplicate", MemberDescriptorAccess.CanRead)
            );

            Assert.That(
                () =>
                    descriptor.AddMember(
                        "Duplicate",
                        StubMemberDescriptor.CreateCallable("Duplicate", MemberDescriptorAccess.CanRead)
                    ),
                Throws.ArgumentException.With.Message.Contains("Multiple members named Duplicate")
            );
        }

        [Test]
        public void AddMemberAggregatesOverloadableDescriptors()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            StubOverloadableMemberDescriptor first = new("Describe", "first");
            StubOverloadableMemberDescriptor second = new("Describe", "second");

            descriptor.AddMember("Describe", first);
            descriptor.AddMember("Describe", second);

            IMemberDescriptor result = descriptor.FindMember("Describe");

            Assert.That(result, Is.TypeOf<OverloadedMethodMemberDescriptor>());
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

        [Test]
        public void SetIndexUsesIndexerSetterForBracketSyntax()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            bool invoked = false;
            descriptor.AddMember(
                IndexerSetterName,
                StubMemberDescriptor.CreateCallable(
                    IndexerSetterName,
                    MemberDescriptorAccess.CanExecute,
                    getter: (_, _) =>
                        DynValue.NewCallback(
                            (_, args) =>
                            {
                                invoked = true;
                                Assert.That(args[0].Number, Is.EqualTo(2));
                                Assert.That(args[1].String, Is.EqualTo("payload"));
                                return DynValue.Nil;
                            }
                        )
                )
            );

            bool handled = descriptor.SetIndex(
                script,
                new DescriptorHost(),
                DynValue.NewNumber(2),
                DynValue.NewString("payload"),
                isDirectIndexing: false
            );

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(invoked, Is.True);
            });
        }

        [Test]
        public void SetIndexFallsBackToNamedMemberWhenDirectIndexing()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            bool invoked = false;
            descriptor.AddMember(
                "DirectTarget",
                StubMemberDescriptor.CreateCallable(
                    "DirectTarget",
                    MemberDescriptorAccess.CanWrite,
                    getter: (_, _) => DynValue.Nil,
                    setter: (_, _, value) =>
                    {
                        invoked = true;
                        Assert.That(value.String, Is.EqualTo("direct"));
                    }
                )
            );

            bool handled = descriptor.SetIndex(
                script,
                new DescriptorHost(),
                DynValue.NewString("DirectTarget"),
                DynValue.NewString("direct"),
                isDirectIndexing: true
            );

            Assert.Multiple(() =>
            {
                Assert.That(handled, Is.True);
                Assert.That(invoked, Is.True);
            });
        }

        [Test]
        public void SetIndexReturnsFalseWhenNonStringIndexProvided()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();

            bool handled = descriptor.SetIndex(
                script,
                new DescriptorHost(),
                DynValue.NewNumber(5),
                DynValue.NewString("ignored"),
                isDirectIndexing: true
            );

            Assert.That(handled, Is.False);
        }

        [Test]
        public void SetIndexReturnsFalseWhenMemberIsMissing()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();

            bool handled = descriptor.SetIndex(
                script,
                new DescriptorHost(),
                DynValue.NewString("DoesNotExist"),
                DynValue.NewNumber(1),
                isDirectIndexing: true
            );

            Assert.That(handled, Is.False);
        }

        [Test]
        public void OptimizeDelegatesToMembersAndMetaMembers()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            bool memberOptimized = false;
            bool metaOptimized = false;
            descriptor.AddMember(
                "OptimizableMember",
                new StubOptimizableMemberDescriptor("OptimizableMember", () => memberOptimized = true)
            );
            descriptor.AddMetaMember(
                "__meta",
                new StubOptimizableMemberDescriptor("__meta", () => metaOptimized = true)
            );

            ((IOptimizableDescriptor)descriptor).Optimize();

            Assert.Multiple(() =>
            {
                Assert.That(memberOptimized, Is.True);
                Assert.That(metaOptimized, Is.True);
            });
        }

        [Test]
        public void HelperNameTransformationsMirrorDescriptorHelpers()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    AccessibleDispatchingUserDataDescriptor.InvokeCamelify("hello_world-value"),
                    Is.EqualTo(DescriptorHelpers.Camelify("hello_world-value"))
                );
                Assert.That(
                    AccessibleDispatchingUserDataDescriptor.InvokeUpperFirst("example"),
                    Is.EqualTo(DescriptorHelpers.UpperFirstLetter("example"))
                );
            });
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
            private readonly Action<Script, object, DynValue> setter;

            private StubMemberDescriptor(
                string name,
                MemberDescriptorAccess access,
                bool isStatic,
                Func<Script, object, DynValue> getter,
                Action<Script, object, DynValue> setter
            )
            {
                Name = name;
                MemberAccess = access;
                IsStatic = isStatic;
                this.getter = getter ?? ((_, _) => DynValue.Nil);
                this.setter = setter ?? ((_, _, _) => throw new NotSupportedException());
            }

            public static StubMemberDescriptor CreateCallable(
                string name,
                MemberDescriptorAccess access,
                Func<Script, object, DynValue> getter = null,
                Action<Script, object, DynValue> setter = null
            )
            {
                return new StubMemberDescriptor(
                    name,
                    access,
                    isStatic: false,
                    getter: getter ?? ((_, _) => DynValue.NewCallback((_, _) => DynValue.Nil)),
                    setter: setter
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
                setter(script, obj, value);
            }
        }

        private sealed class StubOverloadableMemberDescriptor : IOverloadableMemberDescriptor
        {
            private readonly DynValue result;

            public StubOverloadableMemberDescriptor(string name, string discriminant)
            {
                Name = name;
                SortDiscriminant = discriminant;
                MemberAccess = MemberDescriptorAccess.CanExecute;
                result = DynValue.NewString(discriminant);
            }

            public bool IsStatic { get; } = false;

            public string Name { get; }

            public MemberDescriptorAccess MemberAccess { get; }

            public Type ExtensionMethodType => null;

            public IReadOnlyList<ParameterDescriptor> Parameters { get; } =
                Array.Empty<ParameterDescriptor>();

            public Type VarArgsArrayType => null;

            public Type VarArgsElementType => null;

            public string SortDiscriminant { get; }

            public DynValue Execute(
                Script script,
                object obj,
                ScriptExecutionContext context,
                CallbackArguments args
            )
            {
                return result;
            }

            public DynValue GetValue(Script script, object obj)
            {
                return DynValue.NewCallback((_, _) => result);
            }

            public void SetValue(Script script, object obj, DynValue value)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class StubOptimizableMemberDescriptor
            : IMemberDescriptor,
                IOptimizableDescriptor
        {
            private readonly Action optimize;

            public StubOptimizableMemberDescriptor(string name, Action optimize)
            {
                Name = name;
                MemberAccess = MemberDescriptorAccess.CanRead;
                this.optimize = optimize;
            }

            public bool IsStatic { get; } = false;

            public string Name { get; }

            public MemberDescriptorAccess MemberAccess { get; }

            public DynValue GetValue(Script script, object obj)
            {
                return DynValue.Nil;
            }

            public void SetValue(Script script, object obj, DynValue value)
            {
                throw new NotSupportedException();
            }

            public void Optimize()
            {
                optimize?.Invoke();
            }
        }

        private sealed class AccessibleDispatchingUserDataDescriptor
            : DispatchingUserDataDescriptor
        {
            public AccessibleDispatchingUserDataDescriptor()
                : base(typeof(DescriptorHost)) { }

            public static string InvokeCamelify(string name)
            {
                return Camelify(name);
            }

            public static string InvokeUpperFirst(string name)
            {
                return UpperFirstLetter(name);
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
