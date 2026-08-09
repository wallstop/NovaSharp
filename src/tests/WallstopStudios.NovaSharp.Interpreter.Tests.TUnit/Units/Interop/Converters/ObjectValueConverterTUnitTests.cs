namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop.Converters
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NovaSharp;
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
            LuaValue fallback = LuaValue.NewString("missing");

            LuaValue result = ObjectValueConverter.SerializeObjectToDynValue(
                script,
                null,
                fallback
            );
            LuaValue defaultResult = ObjectValueConverter.SerializeObjectToDynValue(script, null);
            LuaValue explicitNil = ObjectValueConverter.SerializeObjectToDynValue(
                script,
                null,
                LuaValue.Nil
            );
            LuaValue legacyNull = ObjectValueConverter.SerializeObjectToDynValue(
                script,
                null,
                null
            );
            LuaValue explicitVoid = ObjectValueConverter.SerializeObjectToDynValue(
                script,
                null,
                LuaValue.Void
            );

            await Assert.That(result).IsEqualTo(fallback).ConfigureAwait(false);
            await Assert.That(defaultResult.IsNil).IsTrue().ConfigureAwait(false);
            await Assert.That(explicitNil.IsNil).IsTrue().ConfigureAwait(false);
            await Assert.That(legacyNull.IsNil).IsTrue().ConfigureAwait(false);
            await Assert.That(explicitVoid.IsVoid()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SerializeObjectToDynValueCapturesInstanceAndStaticProperties()
        {
            Script script = new();
            SampleObject sample = new("value");

            LuaValue result = ObjectValueConverter.SerializeObjectToDynValue(script, sample);
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
            LuaValue fallback = LuaValue.NewString("missing");

            LuaValue result = ObjectValueConverter.SerializeObjectToDynValue(
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
