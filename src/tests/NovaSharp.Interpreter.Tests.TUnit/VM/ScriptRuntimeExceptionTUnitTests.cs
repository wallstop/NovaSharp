namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Tests;

    [ScriptGlobalOptionsIsolation]
    public sealed class ScriptRuntimeExceptionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TableIndexFactoriesReturnStockMessages()
        {
            await Assert
                .That(ScriptRuntimeException.TableIndexIsNil().Message)
                .IsEqualTo("table index is nil");
            await Assert
                .That(ScriptRuntimeException.TableIndexIsNaN().Message)
                .IsEqualTo("table index is NaN");
        }

        [global::TUnit.Core.Test]
        public async Task ConvertToNumberFailedReturnsStageSpecificMessage()
        {
            Dictionary<int, string> cases = new()
            {
                { 0, "value must be a number" },
                { 1, "'for' initial value must be a number" },
                { 2, "'for' step must be a number" },
                { 3, "'for' limit must be a number" },
                { 42, "value must be a number" },
            };

            foreach (KeyValuePair<int, string> entry in cases)
            {
                ScriptRuntimeException exception = ScriptRuntimeException.ConvertToNumberFailed(
                    entry.Key
                );
                await Assert.That(exception.Message).IsEqualTo(entry.Value);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ConvertObjectFailedIncludesClrAndLuaTypes()
        {
            ScriptRuntimeException clrException = ScriptRuntimeException.ConvertObjectFailed(
                new SampleClrType()
            );
            ScriptRuntimeException luaException = ScriptRuntimeException.ConvertObjectFailed(
                DataType.String
            );

            await Assert
                .That(clrException.Message)
                .IsEqualTo($"cannot convert clr type {typeof(SampleClrType)}");
            await Assert
                .That(luaException.Message)
                .IsEqualTo("cannot convert a string to a clr type");
        }

        [global::TUnit.Core.Test]
        public async Task UserDataArgumentTypeMismatchHighlightsTypes()
        {
            ScriptRuntimeException exception = ScriptRuntimeException.UserDataArgumentTypeMismatch(
                DataType.Table,
                typeof(string)
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo(
                    "cannot find a conversion from a NovaSharp table to a clr System.String"
                );
        }

        [global::TUnit.Core.Test]
        public async Task CannotResumeNotSuspendedDifferentiatesStates()
        {
            Dictionary<CoroutineState, string> cases = new()
            {
                { CoroutineState.Dead, "cannot resume dead coroutine" },
                { CoroutineState.Running, "cannot resume non-suspended coroutine" },
                { CoroutineState.ForceSuspended, "cannot resume non-suspended coroutine" },
            };

            foreach (KeyValuePair<CoroutineState, string> entry in cases)
            {
                ScriptRuntimeException exception = ScriptRuntimeException.CannotResumeNotSuspended(
                    entry.Key
                );
                await Assert.That(exception.Message).IsEqualTo(entry.Value);
            }
        }

        [global::TUnit.Core.Test]
        public async Task CannotYieldMessagesMatchLuaText()
        {
            await Assert
                .That(ScriptRuntimeException.CannotYield().Message)
                .IsEqualTo("attempt to yield across a CLR-call boundary");
            await Assert
                .That(ScriptRuntimeException.CannotYieldMain().Message)
                .IsEqualTo("attempt to yield from outside a coroutine");
        }

        [global::TUnit.Core.Test]
        public async Task AccessInstanceMemberOnStaticsValidatesDescriptors()
        {
            IMemberDescriptor descriptor = new StubMemberDescriptor("Length", isStatic: false);
            IUserDataDescriptor typeDescriptor = new StubUserDataDescriptor("Vector3");

            ScriptRuntimeException memberOnly =
                ScriptRuntimeException.AccessInstanceMemberOnStatics(descriptor);
            ScriptRuntimeException typed = ScriptRuntimeException.AccessInstanceMemberOnStatics(
                typeDescriptor,
                descriptor
            );

            await Assert
                .That(memberOnly.Message)
                .IsEqualTo("attempt to access instance member Length from a static userdata");
            await Assert
                .That(typed.Message)
                .IsEqualTo(
                    "attempt to access instance member Vector3.Length from a static userdata"
                );
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }

        private sealed class SampleClrType { }

        private sealed class StubMemberDescriptor : IMemberDescriptor
        {
            public StubMemberDescriptor(string name, bool isStatic)
            {
                Name = name;
                IsStatic = isStatic;
            }

            public bool IsStatic { get; }

            public string Name { get; }

            public MemberDescriptorAccess MemberAccess => MemberDescriptorAccess.CanRead;

            public DynValue GetValue(Script script, object obj) =>
                throw new NotSupportedException();

            public void SetValue(Script script, object obj, DynValue value) =>
                throw new NotSupportedException();
        }

        private sealed class StubUserDataDescriptor : IUserDataDescriptor
        {
            public StubUserDataDescriptor(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public Type Type => typeof(object);

            public DynValue Index(
                Script script,
                object obj,
                DynValue index,
                bool isDirectIndexing
            ) => throw new NotSupportedException();

            public bool SetIndex(
                Script script,
                object obj,
                DynValue index,
                DynValue value,
                bool isDirectIndexing
            ) => throw new NotSupportedException();

            public string AsString(object obj) => throw new NotSupportedException();

            public DynValue MetaIndex(Script script, object obj, string metaname) =>
                throw new NotSupportedException();

            public bool IsTypeCompatible(Type type, object obj) =>
                throw new NotSupportedException();
        }
    }
}
