namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ObjectCallbackMemberDescriptorTests
    {
        private static readonly CallbackArguments NoOpArgs = new CallbackArguments(
            new List<DynValue> { DynValue.NewNumber(1), DynValue.NewNumber(2) },
            false
        );

        [Test]
        public void ExecuteReturnsCallbackResult()
        {
            Script script = new();
            int invocationCount = 0;
            ObjectCallbackMemberDescriptor descriptor = new(
                "Echo",
                (obj, ctx, args) =>
                {
                    invocationCount++;
                    Assert.That(obj, Is.EqualTo("host"));
                    Assert.That(args.Count, Is.EqualTo(2));
                    return DynValue.NewNumber(42);
                }
            );

            DynValue result = descriptor.Execute(script, "host", context: null, NoOpArgs);

            Assert.Multiple(() =>
            {
                Assert.That(invocationCount, Is.EqualTo(1));
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.EqualTo(42));
            });
        }

        [Test]
        public void ExecuteReturnsVoidWhenCallbackMissing()
        {
            Script script = new();
            ObjectCallbackMemberDescriptor descriptor = new(
                "VoidMember",
                callBack: null,
                parameters: Array.Empty<ParameterDescriptor>()
            );

            DynValue result = descriptor.Execute(script, new object(), null, NoOpArgs);

            Assert.That(result.Type, Is.EqualTo(DataType.Void));
        }
    }
}
