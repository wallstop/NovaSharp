namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;

    public sealed class ObjectCallbackMemberDescriptorTUnitTests
    {
        private static readonly CallbackArguments NoOpArgs = new CallbackArguments(
            new List<DynValue> { DynValue.NewNumber(1), DynValue.NewNumber(2) },
            false
        );

        [global::TUnit.Core.Test]
        public async Task ExecuteReturnsCallbackResult()
        {
            Script script = new();
            int invocationCount = 0;
            object capturedObject = null;
            int capturedArgCount = 0;
            ObjectCallbackMemberDescriptor descriptor = new(
                "Echo",
                (obj, ctx, args) =>
                {
                    invocationCount++;
                    capturedObject = obj;
                    capturedArgCount = args?.Count ?? 0;
                    return DynValue.NewNumber(42);
                }
            );

            DynValue result = descriptor.Execute(script, "host", context: null, NoOpArgs);

            await Assert.That(invocationCount).IsEqualTo(1);
            await Assert.That(capturedObject).IsEqualTo("host");
            await Assert.That(capturedArgCount).IsEqualTo(2);
            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(42d);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteReturnsVoidWhenCallbackMissing()
        {
            Script script = new();
            ObjectCallbackMemberDescriptor descriptor = new(
                "VoidMember",
                callBack: null,
                parameters: Array.Empty<ParameterDescriptor>()
            );

            DynValue result = descriptor.Execute(script, new object(), null, NoOpArgs);

            await Assert.That(result.Type).IsEqualTo(DataType.Void);
        }
    }
}
