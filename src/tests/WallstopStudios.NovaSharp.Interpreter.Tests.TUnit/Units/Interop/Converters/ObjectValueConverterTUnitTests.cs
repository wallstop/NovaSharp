namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop.Converters
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Serialization;

    public sealed class ObjectValueConverterTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task SerializeObjectToDynValueUsesCustomNullReplacement()
        {
            Script script = new();
            DynValue fallback = DynValue.NewString("missing");

            DynValue result = ObjectValueConverter.SerializeObjectToDynValue(
                script,
                null,
                fallback
            );
            DynValue defaultResult = ObjectValueConverter.SerializeObjectToDynValue(script, null);
            DynValue explicitNil = ObjectValueConverter.SerializeObjectToDynValue(
                script,
                null,
                DynValue.Nil
            );
            DynValue legacyNull = ObjectValueConverter.SerializeObjectToDynValue(
                script,
                null,
                null
            );
            DynValue explicitVoid = ObjectValueConverter.SerializeObjectToDynValue(
                script,
                null,
                DynValue.Void
            );

            await Assert.That(result).IsSameReferenceAs(fallback).ConfigureAwait(false);
            await Assert.That(defaultResult.IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(explicitNil.IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(legacyNull.IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(explicitVoid.IsVoid()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeObjectToDynValueCapturesInstanceAndStaticProperties()
        {
            Script script = new();
            SampleObject sample = new("value");

            DynValue result = ObjectValueConverter.SerializeObjectToDynValue(script, sample);
            Table serialized = result.Table;

            await Assert
                .That(serialized.Get(nameof(SampleObject.InstanceValue)).String)
                .IsEqualTo("value")
                .ConfigureAwait(false);
            await Assert
                .That(serialized.Get(nameof(SampleObject.StaticNumber)).Number)
                .IsEqualTo(SampleObject.StaticNumber)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeObjectToDynValueEnumeratesListsAndEnums()
        {
            Script script = new();
            List<object> payload = new() { SampleEnum.Second, null, "tail" };
            DynValue fallback = DynValue.NewString("missing");

            DynValue result = ObjectValueConverter.SerializeObjectToDynValue(
                script,
                payload,
                fallback
            );
            Table serialized = result.Table;

            await Assert.That(serialized.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert
                .That(serialized.Get(1).Number)
                .IsEqualTo((double)SampleEnum.Second)
                .ConfigureAwait(false);
            await Assert.That(serialized.Get(2).String).IsEqualTo("missing").ConfigureAwait(false);
            await Assert.That(serialized.Get(3).String).IsEqualTo("tail").ConfigureAwait(false);
        }

        private sealed class SampleObject
        {
            public SampleObject(string value)
            {
                InstanceValue = value;
            }

            public string InstanceValue { get; }

            public static int StaticNumber => 42;
        }

        private enum SampleEnum
        {
            First = 1,
            Second = 2,
        }
    }
}
