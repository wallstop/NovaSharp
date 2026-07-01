namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [ScriptGlobalOptionsIsolation]
    [UserDataIsolation]
    public sealed class DispatchingUserDataDescriptorTUnitTests
    {
        private const string IndexerGetterName = "get_Item";
        private const string IndexerSetterName = "set_Item";
        private static readonly int[] HostAddSequence = { 1, 2 };
        private static readonly int[] HostOtherSequence = { 3 };
        private static readonly int[] HostCopySequence = { 2 };
        private static readonly int[] HostZeroSequence = { 0 };

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ArithmeticOperatorsDispatchToClrOverloads(LuaCompatibilityVersion version)
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);

            DynValue sum = script.DoString("return (hostAdd + hostOther).value");
            DynValue difference = script.DoString("return (hostOther - hostAdd).value");
            DynValue product = script.DoString("return (hostAdd * hostOther).value");
            DynValue quotient = script.DoString("return (hostOther / hostAdd).value");
            DynValue modulus = script.DoString("return (hostOther % hostAdd).value");
            DynValue negate = script.DoString("return (-hostAdd).value");

            await Assert.That(sum.Number).IsEqualTo(5d);
            await Assert.That(difference.Number).IsEqualTo(1d);
            await Assert.That(product.Number).IsEqualTo(6d);
            await Assert.That(quotient.Number).IsEqualTo(1d);
            await Assert.That(modulus.Number).IsEqualTo(1d);
            await Assert.That(negate.Number).IsEqualTo(-2d);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ComparisonOperatorsUseComparable(LuaCompatibilityVersion version)
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);

            DynValue equality = script.DoString("return hostAdd == hostCopy");
            DynValue lessThan = script.DoString("return hostAdd < hostOther");
            DynValue lessThanOrEqual = script.DoString("return hostAdd <= hostCopy");

            await Assert.That(equality.Boolean).IsTrue();
            await Assert.That(lessThan.Boolean).IsTrue();
            await Assert.That(lessThanOrEqual.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ComparisonMetamethodSupportsSecondOperandOwnership(
            LuaCompatibilityVersion version
        )
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(
                version,
                out DispatchHost hostAdd,
                out DispatchHost hostOther,
                out _,
                out _
            );

            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(hostAdd);
            DynValue meta = descriptor.MetaIndex(script, hostAdd, "__lt");
            await Assert.That(meta).IsNotNull();

            DynValue result = script.Call(
                meta,
                UserData.Create(hostOther),
                UserData.Create(hostAdd)
            );

            await Assert.That(result.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MetaIndexReturnsNullForUnsupportedMetamethod(
            LuaCompatibilityVersion version
        )
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(
                version,
                out DispatchHost hostAdd,
                out _,
                out _,
                out _
            );
            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(hostAdd);

            DynValue meta = descriptor.MetaIndex(script, hostAdd, "__unknown");

            await Assert.That(meta).IsNull();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LenOperatorReturnsCount(LuaCompatibilityVersion version)
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);

            DynValue len = script.DoString("return #hostAdd");

            await Assert.That(len.Number).IsEqualTo(2d);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LenOperatorFallsBackToCountWhenLengthMissing(
            LuaCompatibilityVersion version
        )
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);
            script.Globals["countOnly"] = UserData.Create(new CountOnlyHost(4));

            DynValue len = script.DoString("return #countOnly");

            await Assert.That(len.Number).IsEqualTo(4d);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ComparisonMetamethodReturnsNullWhenObjectIsNotComparable(
            LuaCompatibilityVersion version
        )
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();

            DynValue lt = descriptor.MetaIndex(script, new DescriptorHost(), "__lt");
            DynValue le = descriptor.MetaIndex(script, new DescriptorHost(), "__le");

            await Assert.That(lt).IsNull();
            await Assert.That(le).IsNull();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LenMetamethodReturnsNullWhenTargetIsNull(LuaCompatibilityVersion version)
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();

            DynValue len = descriptor.MetaIndex(script, null, "__len");

            await Assert.That(len).IsNull();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ToNumberDispatchesImplicitConversion(LuaCompatibilityVersion version)
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(
                version,
                out _,
                out DispatchHost hostOther,
                out _,
                out _
            );
            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(hostOther);
            DynValue meta = descriptor.MetaIndex(script, hostOther, "__tonumber");

            await Assert.That(meta).IsNotNull();

            DynValue number = script.Call(meta, UserData.Create(hostOther));

            await Assert.That(number.Type).IsEqualTo(DataType.Number);
            await Assert.That(number.Number).IsEqualTo(3d);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ToNumberSkipsMissingConvertersBeforeFindingMatch(
            LuaCompatibilityVersion version
        )
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);
            IntConversionOnlyHost host = new(9);
            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(host);
            DynValue meta = descriptor.MetaIndex(script, host, "__tonumber");

            await Assert.That(meta).IsNotNull();

            DynValue number = script.Call(meta, UserData.Create(host));

            await Assert.That(number.Type).IsEqualTo(DataType.Number);
            await Assert.That(number.Number).IsEqualTo(9d);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ToNumberReturnsNilWhenNoConvertersExist(LuaCompatibilityVersion version)
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);
            NoConversionHost host = new(6);
            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(host);

            DynValue meta = descriptor.MetaIndex(script, host, "__tonumber");

            await Assert.That(meta).IsNull();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ToBoolUsesImplicitConversion(LuaCompatibilityVersion version)
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(
                version,
                out DispatchHost hostAdd,
                out _,
                out _,
                out DispatchHost hostZero
            );
            IUserDataDescriptor descriptor = UserData.GetDescriptorForObject(hostAdd);
            DynValue meta = descriptor.MetaIndex(script, hostAdd, "__tobool");

            await Assert.That(meta).IsNotNull();

            DynValue trueResult = script.Call(meta, UserData.Create(hostAdd));
            DynValue falseResult = script.Call(meta, UserData.Create(hostZero));

            await Assert.That(trueResult.Boolean).IsTrue();
            await Assert.That(falseResult.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task IteratorDispatchEnumeratesClrEnumerable(LuaCompatibilityVersion version)
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);

            DynValue sum = script.DoString(
                @"
                local total = 0
                for value in hostAdd do
                    total = total + value
                end
                return total"
            );

            await Assert.That(sum.Number).IsEqualTo(3d);
        }

        [global::TUnit.Core.Test]
        public async Task MemberManagementSupportsAddRemoveQueries()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            StubMemberDescriptor member = StubMemberDescriptor.CreateCallable(
                "Value",
                MemberDescriptorAccess.CanRead
            );
            descriptor.AddMember("Value", member);
            descriptor.AddMetaMember("__meta", member);
            descriptor.AddDynValue("dyn", DynValue.NewNumber(5));

            IEnumerable<string> memberNames = descriptor.MemberNames;
            IEnumerable<KeyValuePair<string, IMemberDescriptor>> members = descriptor.Members;
            IEnumerable<string> metaNames = descriptor.MetaMemberNames;
            IEnumerable<KeyValuePair<string, IMemberDescriptor>> metaMembers =
                descriptor.MetaMembers;

            await Assert.That(memberNames.Contains("Value")).IsTrue();
            await Assert.That(members.Any(kv => kv.Key == "dyn")).IsTrue();
            await Assert.That(metaNames.Contains("__meta")).IsTrue();
            await Assert
                .That(metaMembers.Any(kv => kv.Key == "__meta" && kv.Value == member))
                .IsTrue();
            await Assert.That(descriptor.FindMetaMember("__meta")).IsEqualTo(member);

            descriptor.RemoveMember("Value");
            descriptor.RemoveMetaMember("__meta");

            await Assert.That(descriptor.HasMember("Value")).IsFalse();
            await Assert.That(descriptor.HasMetaMember("__meta")).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task AddMemberThrowsWhenDuplicateNonOverloadAdded()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            descriptor.AddMember(
                "Duplicate",
                StubMemberDescriptor.CreateCallable("Duplicate", MemberDescriptorAccess.CanRead)
            );

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                descriptor.AddMember(
                    "Duplicate",
                    StubMemberDescriptor.CreateCallable("Duplicate", MemberDescriptorAccess.CanRead)
                )
            );

            await Assert.That(exception.Message).Contains("Multiple members named Duplicate");
        }

        [global::TUnit.Core.Test]
        public async Task AddMemberAggregatesOverloadableDescriptors()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            StubOverloadableMemberDescriptor first = new("Describe", "first");
            StubOverloadableMemberDescriptor second = new("Describe", "second");

            descriptor.AddMember("Describe", first);
            descriptor.AddMember("Describe", second);

            IMemberDescriptor result = descriptor.FindMember("Describe");

            await Assert.That(result is OverloadedMethodMemberDescriptor).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task IndexerInvokesClrCallbackWhenAccessedFromBracketSyntax()
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
                            (_, args) =>
                            {
                                invoked = true;
                                return DynValue.NewString($"idx:{args[0].Number}");
                            }
                        )
                )
            );

            Script script = new(CoreModules.Basic | CoreModules.GlobalConsts);
            DynValue result = descriptor.Index(
                script,
                new DescriptorHost(),
                DynValue.NewNumber(7),
                isDirectIndexing: false
            );

            await Assert.That(invoked).IsTrue();
            await Assert.That(result.String).IsEqualTo("idx:7");
        }

        [global::TUnit.Core.Test]
        public async Task IndexerThrowsWhenGetterIsNotClrCallback()
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

            Script script = new(CoreModules.Basic | CoreModules.GlobalConsts);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                descriptor.Index(
                    script,
                    new DescriptorHost(),
                    DynValue.NewNumber(1),
                    isDirectIndexing: false
                )
            );

            await Assert.That(exception.Message).Contains("clr callback was expected");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task IndexFallsBackToExtensionMethodsAfterRegistration(
            LuaCompatibilityVersion version
        )
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            registrationScope.RegisterExtensionType(typeof(DispatchHostExtensionMethods));
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);
            DynValue result = script.DoString("return hostAdd:DescribeExt()");

            await Assert.That(result.String).IsEqualTo("ext:2");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DivisionByZeroPropagatesInvocationException(
            LuaCompatibilityVersion version
        )
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);

            TargetInvocationException exception = ExpectException<TargetInvocationException>(() =>
                script.DoString("return (hostAdd / hostZero).value")
            );

            await Assert.That(exception.InnerException is DivideByZeroException).IsTrue();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ConcatOperatorFallsBackToRegisteredMetamethod(
            LuaCompatibilityVersion version
        )
        {
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);
            script.Globals["concatLeft"] = UserData.Create(new MetaFallbackHost("L"));
            script.Globals["concatRight"] = UserData.Create(new MetaFallbackHost("R"));

            DynValue concat = script.DoString("return concatLeft .. concatRight");

            await Assert.That(concat.String).IsEqualTo("L|R");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SetIndexUsesIndexerSetterForBracketSyntax(LuaCompatibilityVersion version)
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            bool invoked = false;
            double capturedIndex = 0;
            string capturedValue = string.Empty;
            descriptor.AddMember(
                IndexerSetterName,
                StubMemberDescriptor.CreateCallable(
                    IndexerSetterName,
                    MemberDescriptorAccess.CanExecute,
                    (_, _) =>
                        DynValue.NewCallback(
                            (_, args) =>
                            {
                                invoked = true;
                                capturedIndex = args[0].Number;
                                capturedValue = args[1].String;
                                return DynValue.Nil;
                            }
                        )
                )
            );

            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);

            bool handled = descriptor.SetIndex(
                script,
                new DescriptorHost(),
                DynValue.NewNumber(2),
                DynValue.NewString("payload"),
                isDirectIndexing: false
            );

            await Assert.That(handled).IsTrue();
            await Assert.That(invoked).IsTrue();
            await Assert.That(capturedIndex).IsEqualTo(2d);
            await Assert.That(capturedValue).IsEqualTo("payload");
        }

        [global::TUnit.Core.Test]
        [Arguments(false)]
        [Arguments(true)]
        public async Task NonTupleIndexerCallbackViewAvoidsArgumentArrayAllocation(bool isSetter)
        {
            const int iterations = 1024;
            DescriptorHostDescriptor descriptor = CreateDescriptorHostDescriptor();
            Script script = new(CoreModules.Basic | CoreModules.GlobalConsts);
            DescriptorHost target = new();
            DynValue index = DynValue.NewNumber(3);
            DynValue value = isSetter ? DynValue.NewString("payload") : null;
            DynValue callback = DynValue.NewCallbackView(ReturnLastIndexerArgument);
            StubMemberDescriptor member = StubMemberDescriptor.CreateCallable(
                isSetter ? IndexerSetterName : IndexerGetterName,
                MemberDescriptorAccess.CanExecute,
                (_, _) => callback
            );

            DynValue warmup = descriptor.InvokeExecuteIndexer(member, script, target, index, value);
            await Assert.That(warmup).IsEqualTo(isSetter ? value : index).ConfigureAwait(false);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long allocated = MeasureNonTupleIndexerCallbackViewAllocations(
                descriptor,
                member,
                script,
                target,
                index,
                value,
                iterations
            );
            long allocatedPerCall = allocated / iterations;

            await Assert
                .That(allocatedPerCall)
                .IsLessThan(16L)
                .Because(
                    $"Non-tuple {(isSetter ? "setter" : "getter")} indexer callback-view dispatch allocated {allocated} bytes across {iterations} iterations ({allocatedPerCall} bytes/call)."
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(false)]
        [Arguments(true)]
        public async Task NonTupleIndexerContextfulCallbacksKeepDynamicContextShape(
            bool useCallbackView
        )
        {
            DescriptorHostDescriptor descriptor = CreateDescriptorHostDescriptor();
            Script script = new(CoreModules.Basic | CoreModules.GlobalConsts);
            DescriptorHost target = new();
            DynValue index = DynValue.NewNumber(4);
            InvalidOperationException additionalDataException = null;
            DynValue callback = useCallbackView
                ? DynValue.NewCallbackView(
                    (context, args) =>
                    {
                        additionalDataException = ExpectException<InvalidOperationException>(() =>
                            context.AdditionalData = "payload"
                        );
                        return DynValue.NewBoolean(
                            context.AdditionalData == null && args.Count == 1
                        );
                    }
                )
                : DynValue.NewCallback(
                    (context, args) =>
                    {
                        additionalDataException = ExpectException<InvalidOperationException>(() =>
                            context.AdditionalData = "payload"
                        );
                        return DynValue.NewBoolean(
                            context.AdditionalData == null && args.Count == 1
                        );
                    }
                );
            StubMemberDescriptor member = StubMemberDescriptor.CreateCallable(
                IndexerGetterName,
                MemberDescriptorAccess.CanExecute,
                (_, _) => callback
            );

            DynValue result = descriptor.InvokeExecuteIndexer(
                member,
                script,
                target,
                index,
                value: null
            );

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(additionalDataException).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(false)]
        [Arguments(true)]
        public async Task ExecuteIndexerUsesDirectOverloadWrapperPath(bool isSetter)
        {
            DescriptorHostDescriptor descriptor = CreateDescriptorHostDescriptor();
            Script script = new(CoreModules.Basic | CoreModules.GlobalConsts);
            DescriptorHost target = new();
            DynValue index = DynValue.NewNumber(5);
            DynValue value = isSetter ? DynValue.NewString("payload") : null;
            StubIndexerOverloadMemberDescriptor overload = new(
                isSetter ? IndexerSetterName : IndexerGetterName
            );
            GetValueCountingOverloadedMethodMemberDescriptor member = new(
                isSetter ? IndexerSetterName : IndexerGetterName,
                overload
            );

            DynValue result = descriptor.InvokeExecuteIndexer(member, script, target, index, value);

            await Assert.That(result).IsEqualTo(isSetter ? value : index).ConfigureAwait(false);
            await Assert.That(overload.ExecuteCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(member.GetValueCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [Arguments(false)]
        [Arguments(true)]
        public async Task RegisteredOverloadedIndexerAvoidsCallbackWrapperAllocation(bool isSetter)
        {
            const int iterations = 1024;
            const long maxAllocatedBytesPerCall = 256L;
            DescriptorHostDescriptor descriptor = CreateDescriptorHostDescriptor();
            Script script = new(CoreModules.Basic | CoreModules.GlobalConsts);
            DescriptorHost target = new();
            DynValue index = DynValue.NewNumber(6);
            DynValue value = isSetter ? DynValue.NewString("payload") : null;
            StubIndexerOverloadMemberDescriptor overload = new(
                isSetter ? IndexerSetterName : IndexerGetterName
            );
            descriptor.AddMember(isSetter ? IndexerSetterName : IndexerGetterName, overload);

            DynValue warmup = InvokeRegisteredOverloadedIndexer(
                descriptor,
                script,
                target,
                index,
                value,
                isSetter
            );
            await Assert.That(warmup).IsEqualTo(isSetter ? value : index).ConfigureAwait(false);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long allocated = MeasureRegisteredOverloadedIndexerAllocations(
                descriptor,
                script,
                target,
                index,
                value,
                isSetter,
                iterations
            );
            long allocatedPerCall = allocated / iterations;

            await Assert
                .That(allocatedPerCall)
                .IsLessThan(maxAllocatedBytesPerCall)
                .Because(
                    $"Registered overloaded {(isSetter ? "setter" : "getter")} indexer dispatch allocated {allocated} bytes across {iterations} iterations ({allocatedPerCall} bytes/call)."
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SetIndexFallsBackToNamedMemberWhenDirectIndexing(
            LuaCompatibilityVersion version
        )
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            bool invoked = false;
            string assignedValue = string.Empty;
            descriptor.AddMember(
                "DirectTarget",
                StubMemberDescriptor.CreateCallable(
                    "DirectTarget",
                    MemberDescriptorAccess.CanWrite,
                    getter: (_, _) => DynValue.Nil,
                    setter: (_, _, value) =>
                    {
                        invoked = true;
                        assignedValue = value.String;
                    }
                )
            );

            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);

            bool handled = descriptor.SetIndex(
                script,
                new DescriptorHost(),
                DynValue.NewString("DirectTarget"),
                DynValue.NewString("direct"),
                isDirectIndexing: true
            );

            await Assert.That(handled).IsTrue();
            await Assert.That(invoked).IsTrue();
            await Assert.That(assignedValue).IsEqualTo("direct");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SetIndexReturnsFalseWhenNonStringIndexProvided(
            LuaCompatibilityVersion version
        )
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);

            bool handled = descriptor.SetIndex(
                script,
                new DescriptorHost(),
                DynValue.NewNumber(5),
                DynValue.NewString("ignored"),
                isDirectIndexing: true
            );

            await Assert.That(handled).IsFalse();
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SetIndexReturnsFalseWhenMemberIsMissing(LuaCompatibilityVersion version)
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);

            bool handled = descriptor.SetIndex(
                script,
                new DescriptorHost(),
                DynValue.NewString("DoesNotExist"),
                DynValue.NewNumber(1),
                isDirectIndexing: true
            );

            await Assert.That(handled).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task IndexThrowsWhenScriptIsNull()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                descriptor.Index(
                    script: null,
                    obj: new DescriptorHost(),
                    index: DynValue.NewString("Value"),
                    isDirectIndexing: true
                )
            );

            await Assert.That(exception.ParamName).IsEqualTo("script");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task IndexThrowsWhenIndexIsNull(LuaCompatibilityVersion version)
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                descriptor.Index(script, new DescriptorHost(), index: null, isDirectIndexing: true)
            );

            await Assert.That(exception.ParamName).IsEqualTo("index");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SetIndexThrowsWhenValueIsNull(LuaCompatibilityVersion version)
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                descriptor.SetIndex(
                    script,
                    new DescriptorHost(),
                    DynValue.NewString("Value"),
                    value: null,
                    isDirectIndexing: true
                )
            );

            await Assert.That(exception.ParamName).IsEqualTo("value");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MetaIndexThrowsWhenMetanameIsNull(LuaCompatibilityVersion version)
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            using UserDataRegistrationScope registrationScope = RegisterDispatchHosts();
            Script script = CreateScriptWithHosts(version, out _, out _, out _, out _);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                descriptor.MetaIndex(script, new DescriptorHost(), metaname: null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("metaname");
        }

        [global::TUnit.Core.Test]
        public async Task OptimizeDelegatesToMembersAndMetaMembers()
        {
            DispatchingUserDataDescriptor descriptor = CreateDescriptorHostDescriptor();
            bool memberOptimized = false;
            bool metaOptimized = false;
            descriptor.AddMember(
                "OptimizableMember",
                new StubOptimizableMemberDescriptor(
                    "OptimizableMember",
                    () => memberOptimized = true
                )
            );
            descriptor.AddMetaMember(
                "__meta",
                new StubOptimizableMemberDescriptor("__meta", () => metaOptimized = true)
            );

            ((IOptimizableDescriptor)descriptor).Optimize();

            await Assert.That(memberOptimized).IsTrue();
            await Assert.That(metaOptimized).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task HelperNameTransformationsMirrorDescriptorHelpers()
        {
            _ = new AccessibleDispatchingUserDataDescriptor();
            await Assert
                .That(AccessibleDispatchingUserDataDescriptor.InvokeCamelify("hello_world-value"))
                .IsEqualTo(DescriptorHelpers.Camelify("hello_world-value"));
            await Assert
                .That(AccessibleDispatchingUserDataDescriptor.InvokeUpperFirst("example"))
                .IsEqualTo(DescriptorHelpers.UpperFirstLetter("example"));
        }

        private static Script CreateScriptWithHosts(
            LuaCompatibilityVersion version,
            out DispatchHost hostAdd,
            out DispatchHost hostOther,
            out DispatchHost hostCopy,
            out DispatchHost hostZero
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            hostAdd = new DispatchHost(2, HostAddSequence);
            hostOther = new DispatchHost(3, HostOtherSequence);
            hostCopy = new DispatchHost(2, HostCopySequence);
            hostZero = new DispatchHost(0, HostZeroSequence);

            script.Globals["hostAdd"] = UserData.Create(hostAdd);
            script.Globals["hostOther"] = UserData.Create(hostOther);
            script.Globals["hostCopy"] = UserData.Create(hostCopy);
            script.Globals["hostZero"] = UserData.Create(hostZero);
            return script;
        }

        private static UserDataRegistrationScope RegisterDispatchHosts()
        {
            UserDataRegistrationScope scope = UserDataRegistrationScope.Create();
            scope.RegisterType<DispatchHost>(
                InteropAccessMode.Reflection,
                ensureUnregistered: true
            );
            scope.RegisterType<MetaFallbackHost>(
                InteropAccessMode.Reflection,
                ensureUnregistered: true
            );
            scope.RegisterType<CountOnlyHost>(
                InteropAccessMode.Reflection,
                ensureUnregistered: true
            );
            scope.RegisterType<IntConversionOnlyHost>(
                InteropAccessMode.Reflection,
                ensureUnregistered: true
            );
            scope.RegisterType<NoConversionHost>(
                InteropAccessMode.Reflection,
                ensureUnregistered: true
            );
            return scope;
        }

        private static DescriptorHostDescriptor CreateDescriptorHostDescriptor()
        {
            return new DescriptorHostDescriptor();
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }

        private static DynValue ReturnLastIndexerArgument(CallbackArgumentsView args)
        {
            return args.Count switch
            {
                1 => args[0],
                2 => args[1],
                _ => throw new ArgumentOutOfRangeException(nameof(args)),
            };
        }

        private static long MeasureNonTupleIndexerCallbackViewAllocations(
            DescriptorHostDescriptor descriptor,
            IMemberDescriptor member,
            Script script,
            DescriptorHost target,
            DynValue index,
            DynValue value,
            int iterations
        )
        {
            DynValue expected = value == null ? index : value;
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                DynValue result = descriptor.InvokeExecuteIndexer(
                    member,
                    script,
                    target,
                    index,
                    value
                );

                if (result != expected)
                {
                    throw new InvalidOperationException(
                        "Non-tuple userdata indexer allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureRegisteredOverloadedIndexerAllocations(
            DescriptorHostDescriptor descriptor,
            Script script,
            DescriptorHost target,
            DynValue index,
            DynValue value,
            bool isSetter,
            int iterations
        )
        {
            DynValue expected = isSetter ? value : index;
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                DynValue result = InvokeRegisteredOverloadedIndexer(
                    descriptor,
                    script,
                    target,
                    index,
                    value,
                    isSetter
                );

                if (result != expected)
                {
                    throw new InvalidOperationException(
                        "Registered overloaded userdata indexer allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static DynValue InvokeRegisteredOverloadedIndexer(
            DescriptorHostDescriptor descriptor,
            Script script,
            DescriptorHost target,
            DynValue index,
            DynValue value,
            bool isSetter
        )
        {
            if (isSetter)
            {
                bool handled = descriptor.SetIndex(
                    script,
                    target,
                    index,
                    value,
                    isDirectIndexing: false
                );
                if (!handled)
                {
                    throw new InvalidOperationException(
                        "Registered overloaded userdata indexer setter was not handled."
                    );
                }

                return value;
            }

            return descriptor.Index(script, target, index, isDirectIndexing: false);
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
            private readonly Func<Script, object, DynValue> _getter;
            private readonly Action<Script, object, DynValue> _setter;

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
                _getter = getter ?? ((_, _) => DynValue.Nil);
                _setter = setter ?? ((_, _, _) => throw new NotSupportedException());
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

            public DynValue GetValue(Script currentScript, object obj)
            {
                return _getter(currentScript, obj);
            }

            public void SetValue(Script currentScript, object obj, DynValue newValue)
            {
                _setter(currentScript, obj, newValue);
            }
        }

        private sealed class StubOverloadableMemberDescriptor : IOverloadableMemberDescriptor
        {
            private readonly DynValue _result;

            public StubOverloadableMemberDescriptor(string name, string discriminant)
            {
                Name = name;
                SortDiscriminant = discriminant;
                MemberAccess = MemberDescriptorAccess.CanExecute;
                _result = DynValue.NewString(discriminant);
            }

            public bool IsStatic { get; }

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
                return _result;
            }

            public DynValue GetValue(Script script, object obj)
            {
                return DynValue.NewCallback((_, _) => _result);
            }

            public void SetValue(Script script, object obj, DynValue value)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class StubIndexerOverloadMemberDescriptor : IOverloadableMemberDescriptor
        {
            public StubIndexerOverloadMemberDescriptor(string name)
            {
                Name = name;
                MemberAccess = MemberDescriptorAccess.CanExecute;
                SortDiscriminant = name;
            }

            public int ExecuteCount { get; private set; }

            public int GetValueCount { get; private set; }

            public bool IsStatic { get; }

            public string Name { get; }

            public MemberDescriptorAccess MemberAccess { get; }

            public Type ExtensionMethodType
            {
                get { return null; }
            }

            public IReadOnlyList<ParameterDescriptor> Parameters { get; } =
                Array.Empty<ParameterDescriptor>();

            public Type VarArgsArrayType
            {
                get { return null; }
            }

            public Type VarArgsElementType
            {
                get { return null; }
            }

            public string SortDiscriminant { get; }

            public DynValue Execute(
                Script script,
                object obj,
                ScriptExecutionContext context,
                CallbackArguments args
            )
            {
                this.CheckAccess(MemberDescriptorAccess.CanExecute, obj);
                ExecuteCount += 1;
                return args[args.Count - 1];
            }

            public DynValue GetValue(Script script, object obj)
            {
                GetValueCount += 1;
                return DynValue.NewCallback((_, args) => args[args.Count - 1]);
            }

            public void SetValue(Script script, object obj, DynValue value)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class GetValueCountingOverloadedMethodMemberDescriptor
            : OverloadedMethodMemberDescriptor,
                IMemberDescriptor
        {
            public GetValueCountingOverloadedMethodMemberDescriptor(
                string name,
                IOverloadableMemberDescriptor descriptor
            )
                : base(name, typeof(DescriptorHost), descriptor) { }

            public int GetValueCount { get; private set; }

            public new DynValue GetValue(Script script, object obj)
            {
                GetValueCount += 1;
                return base.GetValue(script, obj);
            }
        }

        private sealed class StubOptimizableMemberDescriptor
            : IMemberDescriptor,
                IOptimizableDescriptor
        {
            private readonly Action _optimize;

            public StubOptimizableMemberDescriptor(string name, Action optimize)
            {
                Name = name;
                MemberAccess = MemberDescriptorAccess.CanRead;
                _optimize = optimize;
            }

            public bool IsStatic { get; }

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
                _optimize?.Invoke();
            }
        }

        private sealed class AccessibleDispatchingUserDataDescriptor : DispatchingUserDataDescriptor
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

        private sealed class DescriptorHostDescriptor : DispatchingUserDataDescriptor
        {
            public DescriptorHostDescriptor()
                : base(typeof(DescriptorHost)) { }

            public DynValue InvokeExecuteIndexer(
                IMemberDescriptor member,
                Script script,
                object obj,
                DynValue index,
                DynValue value
            )
            {
                return ExecuteIndexer(member, script, obj, index, value);
            }
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
        private readonly int _value;
        private readonly IList<int> _sequence;

        public DispatchHost(int value, IEnumerable<int> sequence)
        {
            _value = value;
            _sequence = new List<int>(sequence);
        }

        public int Value => _value;

        public int Length => _sequence.Count;

        public int Count => _sequence.Count;

        public static DispatchHost operator +(DispatchHost left, DispatchHost right)
        {
            return new DispatchHost(left._value + right._value, left._sequence);
        }

        public static DispatchHost operator -(DispatchHost left, DispatchHost right)
        {
            return new DispatchHost(left._value - right._value, left._sequence);
        }

        public static DispatchHost operator *(DispatchHost left, DispatchHost right)
        {
            return new DispatchHost(left._value * right._value, left._sequence);
        }

        public static DispatchHost operator /(DispatchHost left, DispatchHost right)
        {
            return new DispatchHost(left._value / right._value, left._sequence);
        }

        public static DispatchHost operator %(DispatchHost left, DispatchHost right)
        {
            return new DispatchHost(left._value % right._value, left._sequence);
        }

        public static DispatchHost operator -(DispatchHost host)
        {
            return new DispatchHost(-host._value, host._sequence);
        }

        public override bool Equals(object obj)
        {
            return obj is DispatchHost other && other._value == _value;
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public int CompareTo(DispatchHost other)
        {
            return _value.CompareTo(other?._value ?? 0);
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
            return _sequence.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator double(DispatchHost host)
        {
            return host._value;
        }

        public static implicit operator bool(DispatchHost host)
        {
            return host._value != 0;
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
